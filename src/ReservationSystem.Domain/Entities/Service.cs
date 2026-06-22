using ReservationSystem.Domain.Common;

namespace ReservationSystem.Domain.Entities;

/// <summary>
/// A bookable offering (e.g. a haircut or consultation) with a fixed duration
/// and price.
/// </summary>
public class Service : BaseEntity
{
    /// <summary>Human-readable name of the service.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>How long a single appointment for this service lasts, in minutes.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Price charged for the service.</summary>
    public decimal Price { get; set; }

    /// <summary>Whether the service can currently be booked.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Appointments scheduled for this service.</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
