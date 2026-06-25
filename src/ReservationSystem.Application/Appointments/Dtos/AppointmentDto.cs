using System.Linq.Expressions;
using ReservationSystem.Domain.Entities;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Application.Appointments.Dtos;

/// <summary>Public-facing projection of an <see cref="Appointment"/>.</summary>
public record AppointmentDto(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    DateTime CreatedAt)
{
    /// <summary>
    /// EF Core-translatable projection from an <see cref="Appointment"/> (with its
    /// Service and Customer navigations) to a DTO. Reused by all read queries.
    /// </summary>
    public static Expression<Func<Appointment, AppointmentDto>> Projection => a => new AppointmentDto(
        a.Id,
        a.ServiceId,
        a.Service!.Name,
        a.CustomerId,
        a.Customer!.FullName,
        a.Customer!.Email,
        a.StartTime,
        a.EndTime,
        a.Status,
        a.CreatedAt);
}
