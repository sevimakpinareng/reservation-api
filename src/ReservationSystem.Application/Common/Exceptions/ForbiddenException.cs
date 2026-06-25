namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>
/// Raised when an authenticated user attempts an action they are not allowed to
/// perform on a specific resource (HTTP 403).
/// </summary>
public class ForbiddenException(string message)
    : AppException(403, "Forbidden", message);
