using System.Security.Claims;

namespace backend.Infrastructure;

public static class HttpUserRoles
{
    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal user) =>
        user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
}
