namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>
/// Information about the user making the current request, resolved from the
/// authenticated token. Implemented in the API layer over the HTTP context.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or null if unauthenticated.</summary>
    Guid? Id { get; }

    /// <summary>The authenticated user's email, if present.</summary>
    string? Email { get; }

    /// <summary>The authenticated user's role, if present.</summary>
    string? Role { get; }

    /// <summary>True when the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>True when the current user is in any of the given roles.</summary>
    bool IsInRole(params string[] roles);
}
