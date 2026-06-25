using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ReservationSystem.Application.Common.Interfaces;

namespace ReservationSystem.Api.Authorization;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the authenticated request's claims.
/// Mirrors the claim layout produced by the token service (sub/email/role).
/// </summary>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? Id =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public string? Role => Principal?.FindFirstValue("role");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(params string[] roles) =>
        Principal is { } principal && roles.Any(principal.IsInRole);
}
