using ReservationSystem.Application.Appointments.Dtos;
using ReservationSystem.Application.Common.Models;

namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>Appointment booking and lifecycle use cases.</summary>
public interface IAppointmentService
{
    /// <summary>
    /// Books an appointment for the current user. Enforces all business rules
    /// (future start, active service, no overlap) and computes the end time.
    /// </summary>
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a single appointment, honouring ownership/role visibility.</summary>
    Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged list. Customers see only their own appointments;
    /// BusinessOwner/Admin see all (subject to the supplied filters).
    /// </summary>
    Task<PagedResult<AppointmentDto>> GetAllAsync(AppointmentQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Confirms a pending appointment (BusinessOwner/Admin).</summary>
    Task<AppointmentDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Completes a confirmed appointment (BusinessOwner/Admin).</summary>
    Task<AppointmentDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending or confirmed appointment. Customers may cancel their own;
    /// BusinessOwner/Admin may cancel any.
    /// </summary>
    Task<AppointmentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
