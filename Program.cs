using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Enterprise.Rag.Application;
using Enterprise.Rag.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Rag.Query", policy => policy.RequireAuthenticatedUser().RequireRole("Rag.Query", "Rag.Admin"))
    .AddPolicy("Rag.Ingest", policy => policy.RequireAuthenticatedUser().RequireRole("Rag.Ingest", "Rag.Admin"));
builder.Services.AddRagInfrastructure(builder.Configuration);

var monitorConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(monitorConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        options.ConnectionString = monitorConnectionString;
        options.Credential = new DefaultAzureCredential();
    });
}

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/health/ready", async (IRagReadiness readiness, CancellationToken cancellationToken) =>
{
    var result = await readiness.CheckAsync(cancellationToken).ConfigureAwait(false);
    return result.IsReady
        ? Results.Ok(new { status = "Ready" })
        : Results.Problem(
            title: "RAG platform is not ready",
            detail: string.Join("; ", result.MissingComponents),
            statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapControllers();

app.Run();
