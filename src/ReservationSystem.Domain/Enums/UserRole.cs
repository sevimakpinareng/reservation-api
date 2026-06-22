namespace ReservationSystem.Domain.Enums;

/// <summary>
/// Authorization role assigned to a <see cref="Entities.User"/>.
/// </summary>
public enum UserRole
{
    /// <summary>End user who books appointments.</summary>
    Customer = 0,

    /// <summary>Owner who manages services and their appointments.</summary>
    BusinessOwner = 1,

    /// <summary>Full administrative access.</summary>
    Admin = 2,
}
