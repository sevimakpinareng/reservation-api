using ReservationSystem.Domain.Common;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Domain.Entities;

/// <summary>
/// A booking that links a customer to a service for a specific time window.
/// All times are stored in UTC.
/// </summary>
public class Appointment : BaseEntity
{
    /// <summary>Foreign key to the booking <see cref="User"/> (customer).</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Navigation to the booking customer.</summary>
    public User? Customer { get; set; }

    /// <summary>Foreign key to the booked <see cref="Service"/>.</summary>
    public Guid ServiceId { get; set; }

    /// <summary>Navigation to the booked service.</summary>
    public Service? Service { get; set; }

    /// <summary>Start of the appointment window (UTC).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End of the appointment window (UTC).</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
}
