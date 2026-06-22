namespace ReservationSystem.Application.Authentication;

/// <summary>
/// Strongly-typed JWT settings bound from the <c>Jwt</c> configuration section.
/// The <see cref="Secret"/> must come from a secure source (user-secrets or an
/// environment variable) — never from committed configuration.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes (short-lived).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days (long-lived).</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
