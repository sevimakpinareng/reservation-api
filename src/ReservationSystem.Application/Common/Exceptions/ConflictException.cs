namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>Raised when a request conflicts with existing state (HTTP 409).</summary>
public class ConflictException(string message)
    : AppException(409, "Conflict", message);
