using System.Text;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Enterprise.Rag.Application;
using HtmlAgilityPack;
using UglyToad.PdfPig;

namespace Enterprise.Rag.Infrastructure.Ingestion;

public sealed class DocumentTextExtractor(DocumentIntelligenceClient? documentIntelligenceClient) : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".txt" => await ExtractTextAsync(content, cancellationToken).ConfigureAwait(false),
            ".html" or ".htm" => ExtractHtml(content),
            ".docx" => ExtractDocx(content),
            ".pdf" => await ExtractPdfAsync(content, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"The file type '{Path.GetExtension(fileName)}' is not supported.")
        };
    }

    private static async Task<string> ExtractTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string ExtractHtml(Stream content)
    {
        var document = new HtmlDocument();
        document.Load(content);
        document.DocumentNode.SelectNodes("//script|//style|//noscript")?.ToList().ForEach(node => node.Remove());
        return HtmlEntity.DeEntitize(document.DocumentNode.InnerText);
    }

    private static string ExtractDocx(Stream content)
    {
        using var document = WordprocessingDocument.Open(content, false);
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidDataException("The DOCX file does not contain a document body.");
        return string.Join(
            Environment.NewLine,
            body.Descendants<Paragraph>().Select(paragraph => string.Concat(paragraph.Descendants<Text>().Select(text => text.Text))));
    }

    private async Task<string> ExtractPdfAsync(Stream content, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        string extractedText;
        using (var document = PdfDocument.Open(buffer))
        {
            extractedText = string.Join(
                Environment.NewLine,
                document.GetPages()
                    .Where(page => !string.IsNullOrWhiteSpace(page.Text))
                    .Select(page => $"[[PAGE:{page.Number}]]\n{page.Text}"));
        }

        if (!string.IsNullOrWhiteSpace(extractedText))
        {
            return extractedText;
        }

        if (documentIntelligenceClient is null)
        {
            throw new RagNotConfiguredException(
                "This PDF has no selectable text. Scanned or image-only PDFs require Azure AI Document Intelligence for OCR.");
        }

        buffer.Position = 0;
        var operation = await documentIntelligenceClient
            .AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", BinaryData.FromStream(buffer), cancellationToken)
            .ConfigureAwait(false);

        var pages = operation.Value.Pages;
        if (pages is null || pages.Count == 0)
        {
            throw new InvalidDataException("Azure AI Document Intelligence returned no PDF pages.");
        }

        return string.Join(
            Environment.NewLine,
            pages.Select(page => $"[[PAGE:{page.PageNumber}]]\n{string.Join(' ', page.Lines?.Select(line => line.Content) ?? [])}"));
    }
}
