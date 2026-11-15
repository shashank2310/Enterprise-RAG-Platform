using System.Text.Json;
using Enterprise.Rag.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise_RAG_Platform.Controllers;

[ApiController]
[Authorize(Policy = "Rag.Query")]
[Route("api/rag")]
public sealed class RagController(IRagOrchestrator orchestrator) : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);
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

        try
        {
            var answer = await orchestrator
                .QueryAsync(new RagQuery(request.Question.Trim()), securityContext, cancellationToken)
                .ConfigureAwait(false);
            return Ok(answer);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                title: "The answer-generation request timed out.",
                detail: "The local model did not respond before the timeout. Try again, or use a smaller/faster chat model.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
    }

    [HttpPost("query/stream")]
    public async Task Stream([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "A question is required.",
                Status = StatusCodes.Status400BadRequest
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var securityContext = RagSecurityContextFactory.FromClaimsPrincipal(User);
        if (securityContext is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await WriteStreamEventAsync(new QueryStreamEvent("status", "Preparing your request…"), cancellationToken).ConfigureAwait(false);
            var answer = await orchestrator.QueryAsync(
                new RagQuery(request.Question.Trim()),
                securityContext,
                cancellationToken,
                (message, token) => new ValueTask(WriteStreamEventAsync(new QueryStreamEvent("status", message), token)),
                (text, token) => new ValueTask(WriteStreamEventAsync(new QueryStreamEvent("token", Text: text), token))).ConfigureAwait(false);
            await WriteStreamEventAsync(new QueryStreamEvent("complete", Answer: answer), cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await WriteStreamEventAsync(
                new QueryStreamEvent("error", "The model took too long to respond. Try again or choose a faster local model."),
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            await WriteStreamEventAsync(
                new QueryStreamEvent("error", "Could not reach the model service. Confirm Ollama is running and the configured models are available."),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteStreamEventAsync(QueryStreamEvent streamEvent, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(streamEvent, StreamJsonOptions);
        await Response.Body.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await Response.Body.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public sealed record QueryRequest(string Question);
    private sealed record QueryStreamEvent(string Type, string? Message = null, string? Text = null, RagAnswer? Answer = null);
}
