namespace ReservationSystem.Application.Authentication.Dtos;

/// <summary>Payload for authenticating with email and password.</summary>
public record LoginRequest(string Email, string Password);
