namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>Raised when a requested resource does not exist (HTTP 404).</summary>
public class NotFoundException(string message)
    : AppException(404, "Not Found", message);
