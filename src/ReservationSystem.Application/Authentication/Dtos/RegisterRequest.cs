namespace ReservationSystem.Application.Authentication.Dtos;

/// <summary>Payload for registering a new account.</summary>
public record RegisterRequest(string Email, string Password, string FullName);
