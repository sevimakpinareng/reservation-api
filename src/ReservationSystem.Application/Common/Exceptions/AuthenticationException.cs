namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>
/// Raised when credentials or a token are missing, invalid, expired, or revoked
/// (HTTP 401).
/// </summary>
public class AuthenticationException(string message)
    : AppException(401, "Unauthorized", message);
