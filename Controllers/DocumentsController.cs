using Enterprise.Rag.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise_RAG_Platform.Controllers;

[ApiController]
[Authorize(Policy = "Rag.Ingest")]
[Route("api/documents")]
public sealed class DocumentsController(IDocumentIngestionService ingestionService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    [ProducesResponseType<DocumentIndexResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "A non-empty document file is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var securityContext = RagSecurityContextFactory.FromClaimsPrincipal(User);
        if (securityContext is null)
        {
            return Unauthorized();
        }

        try
        {
            await using var content = file.OpenReadStream();
            var result = await ingestionService.IndexAsync(
                content,
                file.FileName,
                file.ContentType,
                securityContext,
                cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unsupported document type.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The document could not be indexed.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (RagNotConfiguredException exception)
        {
            return Problem(
                title: "A required document processing service is not configured.",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

}
