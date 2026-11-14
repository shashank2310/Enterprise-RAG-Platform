using Enterprise.Rag.Domain.Documents;

namespace Enterprise.Rag.Application.Ingestion;

public sealed class RecursiveTextChunker : IDocumentChunker
{
    public IReadOnlyList<DocumentChunk> Chunk(
        KnowledgeDocument document,
        string extractedText,
        int targetCharacters,
        int overlapCharacters)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(extractedText);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetCharacters, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(overlapCharacters);

        if (overlapCharacters >= targetCharacters)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapCharacters), "Overlap must be smaller than the target chunk size.");
        }

        var normalized = string.Join('\n', extractedText.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));
        var chunks = new List<DocumentChunk>();
        var start = 0;

        while (start < normalized.Length)
        {
            var end = Math.Min(start + targetCharacters, normalized.Length);
            if (end < normalized.Length)
            {
                end = FindBoundary(normalized, start, end);
            }

            var text = normalized[start..end].Trim();
            if (text.Length > 0)
            {
                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["fileName"] = document.FileName,
                    ["contentType"] = document.ContentType,
                    ["tenantId"] = document.TenantId,
                    ["documentId"] = document.Id.ToString("D")
                };
                chunks.Add(new DocumentChunk($"{document.Id:N}-{chunks.Count:D6}", document.Id, document.TenantId, chunks.Count, text, metadata));
            }

            if (end == normalized.Length)
            {
                break;
            }

            start = Math.Max(start + 1, end - overlapCharacters);
            while (start < end && !char.IsWhiteSpace(normalized[start]))
            {
                start++;
            }
        }

        return chunks;
    }

    private static int FindBoundary(string text, int start, int end)
    {
        var minimumUsefulLength = start + (end - start) / 2;
        foreach (var separator in new[] { "\n\n", ". ", " ", "\n" })
        {
            var boundary = text.LastIndexOf(separator, end - 1, end - start, StringComparison.Ordinal);
            if (boundary >= minimumUsefulLength)
            {
                return boundary + separator.Length;
            }
        }

        return end;
    }
}
