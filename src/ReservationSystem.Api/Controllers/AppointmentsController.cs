using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationSystem.Api.Authorization;
using ReservationSystem.Application.Appointments.Dtos;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Application.Common.Models;

namespace ReservationSystem.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    /// <summary>Books an appointment for the current user. The customer comes from the token.</summary>
    [HttpPost]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await appointmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Lists appointments. Customers see only their own; staff see all.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AppointmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> GetAll(
        [FromQuery] AppointmentQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetAllAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single appointment if the caller is allowed to see it.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.GetByIdAsync(id, cancellationToken);
        return Ok(appointment);
    }

    /// <summary>Confirms a pending appointment. Requires BusinessOwner or Admin.</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = AuthorizationPolicies.ManageAppointments)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.ConfirmAsync(id, cancellationToken);
        return Ok(appointment);
    }

    /// <summary>Completes a confirmed appointment. Requires BusinessOwner or Admin.</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.ManageAppointments)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.CompleteAsync(id, cancellationToken);
        return Ok(appointment);
    }

    /// <summary>Cancels an appointment. Customers may cancel their own; staff may cancel any.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.CancelAsync(id, cancellationToken);
        return Ok(appointment);
    }
}
