using ReservationSystem.Domain.Common;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Domain.Entities;

/// <summary>
/// An account that can authenticate against the system. Depending on its
/// <see cref="Role"/> a user books appointments or manages services.
/// </summary>
public class User : BaseEntity
{
    /// <summary>Unique login email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hashed password. Plain-text passwords are never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Display name of the user.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Authorization role.</summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Appointments booked by this user (as the customer).</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
