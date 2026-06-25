namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>
/// Raised when a request violates a domain business rule (HTTP 400), e.g. booking
/// in the past, booking an inactive service, or an invalid status transition.
/// </summary>
public class BusinessRuleException(string message)
    : AppException(400, "Bad Request", message);
