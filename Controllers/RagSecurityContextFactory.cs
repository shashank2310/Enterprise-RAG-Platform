using System.Security.Claims;
using Enterprise.Rag.Application;

namespace Enterprise_RAG_Platform.Controllers;

internal static class RagSecurityContextFactory
{
    public static RagSecurityContext? FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        var tenantId = principal.FindFirstValue("tid");
        var subjectId = principal.FindFirstValue("oid") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(subjectId))
        {
            return null;
        }

        var groups = principal.FindAll("groups")
            .Select(claim => claim.Value)
            .Where(value => Guid.TryParse(value, out _))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new RagSecurityContext(tenantId, subjectId, groups);
    }
}
