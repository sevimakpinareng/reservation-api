using Microsoft.EntityFrameworkCore;
using ReservationSystem.Application.Appointments.Dtos;
using ReservationSystem.Application.Common.Exceptions;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Application.Common.Models;
using ReservationSystem.Domain.Entities;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Application.Appointments.Services;

/// <summary>
/// Appointment booking and lifecycle logic. Enforces the domain's booking rules
/// (future start, active service, no overlapping slot per service), role-based
/// visibility, and the allowed status transitions.
/// </summary>
public class AppointmentService(IApplicationDbContext db, ICurrentUser currentUser) : IAppointmentService
{
    private bool IsPrivileged =>
        currentUser.IsInRole(nameof(UserRole.BusinessOwner), nameof(UserRole.Admin));

    private Guid CurrentUserId =>
        currentUser.Id ?? throw new ForbiddenException("No authenticated user.");

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var startUtc = NormalizeToUtc(request.StartTime);
        if (startUtc <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("Appointments cannot be booked in the past.");
        }

        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Service with id '{request.ServiceId}' was not found.");

        if (!service.IsActive)
        {
            throw new BusinessRuleException("This service is not currently available for booking.");
        }

        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        // Atomic check-then-insert. The application-level overlap check gives a
        // friendly error in the common case; a PostgreSQL exclusion constraint
        // (see the AddAppointmentOverlapConstraint migration) is the hard guard
        // against two concurrent bookings of the same slot.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        if (await HasOverlapAsync(request.ServiceId, startUtc, endUtc, cancellationToken))
        {
            throw new ConflictException("This time slot overlaps an existing appointment for the service.");
        }

        var appointment = new Appointment
        {
            CustomerId = CurrentUserId,
            ServiceId = request.ServiceId,
            StartTime = startUtc,
            EndTime = endUtc,
            Status = AppointmentStatus.Pending,
        };

        db.Appointments.Add(appointment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The overlap exclusion constraint rejected a concurrent double-booking.
            throw new ConflictException("This time slot was just taken. Please pick another time.");
        }

        await transaction.CommitAsync(cancellationToken);

        return await ProjectAsync(appointment.Id, cancellationToken);
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(AppointmentDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound(id);

        if (!IsPrivileged && appointment.CustomerId != CurrentUserId)
        {
            throw new ForbiddenException("You are not allowed to view this appointment.");
        }

        return appointment;
    }

    public async Task<PagedResult<AppointmentDto>> GetAllAsync(AppointmentQueryParameters query, CancellationToken cancellationToken = default)
    {
        var appointments = db.Appointments.AsNoTracking();

        // Customers are restricted to their own appointments.
        if (!IsPrivileged)
        {
            var me = CurrentUserId;
            appointments = appointments.Where(a => a.CustomerId == me);
        }

        if (query.Status is { } status)
        {
            appointments = appointments.Where(a => a.Status == status);
        }

        if (query.ServiceId is { } serviceId)
        {
            appointments = appointments.Where(a => a.ServiceId == serviceId);
        }

        if (query.From is { } from)
        {
            var fromUtc = NormalizeToUtc(from);
            appointments = appointments.Where(a => a.StartTime >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = NormalizeToUtc(to);
            appointments = appointments.Where(a => a.StartTime < toUtc);
        }

        appointments = ApplySorting(appointments, query.SortBy, query.SortDescending);

        var totalCount = await appointments.CountAsync(cancellationToken);

        var items = await appointments
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(AppointmentDto.Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<AppointmentDto>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<AppointmentDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, requireOwnershipForCustomer: false, (appointment) =>
        {
            if (appointment.Status != AppointmentStatus.Pending)
            {
                throw new BusinessRuleException($"Only pending appointments can be confirmed (current status: {appointment.Status}).");
            }

            appointment.Status = AppointmentStatus.Confirmed;
        }, cancellationToken);

    public Task<AppointmentDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, requireOwnershipForCustomer: false, (appointment) =>
        {
            if (appointment.Status != AppointmentStatus.Confirmed)
            {
                throw new BusinessRuleException($"Only confirmed appointments can be completed (current status: {appointment.Status}).");
            }

            appointment.Status = AppointmentStatus.Completed;
        }, cancellationToken);

    public Task<AppointmentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, requireOwnershipForCustomer: true, (appointment) =>
        {
            if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            {
                throw new BusinessRuleException($"Only pending or confirmed appointments can be cancelled (current status: {appointment.Status}).");
            }

            appointment.Status = AppointmentStatus.Cancelled;
        }, cancellationToken);

    /// <summary>
    /// Loads a tracked appointment, applies authorization, runs the supplied
    /// status mutation, persists, and returns the refreshed projection.
    /// </summary>
    private async Task<AppointmentDto> TransitionAsync(
        Guid id,
        bool requireOwnershipForCustomer,
        Action<Appointment> mutate,
        CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw NotFound(id);

        // Confirm/Complete are owner/admin-only (also enforced by the endpoint
        // policy); Cancel additionally lets a customer act on their own booking.
        if (!IsPrivileged && (!requireOwnershipForCustomer || appointment.CustomerId != CurrentUserId))
        {
            throw new ForbiddenException("You are not allowed to modify this appointment.");
        }

        mutate(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(appointment.Id, cancellationToken);
    }

    private Task<bool> HasOverlapAsync(Guid serviceId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken) =>
        db.Appointments.AnyAsync(
            a => a.ServiceId == serviceId
                 && a.Status != AppointmentStatus.Cancelled
                 && a.StartTime < endUtc
                 && a.EndTime > startUtc,
            cancellationToken);

    private async Task<AppointmentDto> ProjectAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(AppointmentDto.Projection)
            .FirstAsync(cancellationToken);

    private static IQueryable<Appointment> ApplySorting(
        IQueryable<Appointment> appointments,
        AppointmentSortBy sortBy,
        bool descending) => (sortBy, descending) switch
    {
        (AppointmentSortBy.CreatedAt, false) => appointments.OrderBy(a => a.CreatedAt),
        (AppointmentSortBy.CreatedAt, true) => appointments.OrderByDescending(a => a.CreatedAt),
        (AppointmentSortBy.Status, false) => appointments.OrderBy(a => a.Status),
        (AppointmentSortBy.Status, true) => appointments.OrderByDescending(a => a.Status),
        (_, true) => appointments.OrderByDescending(a => a.StartTime),
        (_, false) => appointments.OrderBy(a => a.StartTime),
    };

    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static NotFoundException NotFound(Guid id) =>
        new($"Appointment with id '{id}' was not found.");
}
