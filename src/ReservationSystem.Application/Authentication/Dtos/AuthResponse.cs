namespace ReservationSystem.Application.Authentication.Dtos;

/// <summary>
/// Result of a successful authentication: a short-lived access token, a
/// long-lived refresh token, the access token's expiry, and the user profile.
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
