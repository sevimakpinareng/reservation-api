using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Domain.Appointments;

/// <summary>
/// Pure, side-effect-free booking rules. Centralised here so they can be unit
/// tested in isolation and reused by the application services.
/// </summary>
public static class AppointmentRules
{
    /// <summary>The end of an appointment given its start and the service duration.</summary>
    public static DateTime ComputeEndTime(DateTime startUtc, int durationMinutes) =>
        startUtc.AddMinutes(durationMinutes);

    /// <summary>
    /// Whether two half-open time intervals [startA, endA) and [startB, endB)
    /// overlap. Intervals that merely touch at an endpoint do not overlap.
    /// </summary>
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB) =>
        startA < endB && endA > startB;

    /// <summary>Confirm is allowed only from <see cref="AppointmentStatus.Pending"/>.</summary>
    public static bool CanConfirm(AppointmentStatus status) =>
        status == AppointmentStatus.Pending;

    /// <summary>Complete is allowed only from <see cref="AppointmentStatus.Confirmed"/>.</summary>
    public static bool CanComplete(AppointmentStatus status) =>
        status == AppointmentStatus.Confirmed;

    /// <summary>Cancel is allowed from <see cref="AppointmentStatus.Pending"/> or <see cref="AppointmentStatus.Confirmed"/>.</summary>
    public static bool CanCancel(AppointmentStatus status) =>
        status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;
}
