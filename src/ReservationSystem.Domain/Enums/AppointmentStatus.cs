namespace ReservationSystem.Domain.Enums;

/// <summary>
/// Lifecycle state of an <see cref="Entities.Appointment"/>.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Created but not yet confirmed.</summary>
    Pending = 0,

    /// <summary>Confirmed by the business.</summary>
    Confirmed = 1,

    /// <summary>Cancelled by customer or business.</summary>
    Cancelled = 2,

    /// <summary>Service was delivered.</summary>
    Completed = 3,
}
