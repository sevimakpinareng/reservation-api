using ReservationSystem.Domain.Entities;

namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>Produces access and refresh tokens.</summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed JWT access token for the user, embedding the user id,
    /// email, and role claims. Returns the token and its UTC expiry.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>Creates a cryptographically random, opaque refresh token value.</summary>
    string GenerateRefreshToken();
}
