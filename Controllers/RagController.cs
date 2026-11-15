using Enterprise.Rag.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise_RAG_Platform.Controllers;

[ApiController]
[Authorize(Policy = "Rag.Query")]
[Route("api/rag")]
public sealed class RagController(IRagOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("query")]
    [ProducesResponseType<RagAnswer>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "A question is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var securityContext = RagSecurityContextFactory.FromClaimsPrincipal(User);
        if (securityContext is null)
        {
            return Unauthorized();
        }

        var answer = await orchestrator
            .QueryAsync(new RagQuery(request.Question.Trim()), securityContext, cancellationToken)
            .ConfigureAwait(false);
        return Ok(answer);
    }

    public sealed record QueryRequest(string Question);
}
