using ReservationSystem.Application.Authentication.Dtos;

namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>Authentication use cases: registration, login, refresh, logout.</summary>
public interface IAuthService
{
    /// <summary>Registers a new Customer account and issues an initial token pair.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Validates credentials and issues a new token pair.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair, rotating (revoking)
    /// the presented refresh token.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the given refresh token (logout).</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
