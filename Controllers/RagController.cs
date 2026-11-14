using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise_RAG_Platform.Controllers;

[ApiController]
[Authorize(Policy = "Rag.Query")]
[Route("api/rag")]
public sealed class RagController : ControllerBase
{
    [HttpPost("query")]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Query([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "A question is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Problem(
            title: "RAG query orchestration is not configured.",
            detail: "Entra ID validates the bearer token and app role, but the end-to-end query, tenant persistence, and index lifecycle are not enabled in this starter.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    public sealed record QueryRequest(string Question);
}
