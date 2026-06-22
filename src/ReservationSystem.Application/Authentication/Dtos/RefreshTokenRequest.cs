namespace ReservationSystem.Application.Authentication.Dtos;

/// <summary>Payload carrying a refresh token to be exchanged or revoked.</summary>
public record RefreshTokenRequest(string RefreshToken);
