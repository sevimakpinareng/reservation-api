namespace ReservationSystem.Application.Common.Exceptions;

/// <summary>
/// Base type for expected, domain-level errors that map to a specific HTTP
/// status code. The global exception handler translates these into
/// <c>ProblemDetails</c> responses.
/// </summary>
public abstract class AppException(int statusCode, string title, string message)
    : Exception(message)
{
    /// <summary>HTTP status code to return for this error.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Short, human-readable title for the error category.</summary>
    public string Title { get; } = title;
}
