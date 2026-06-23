using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationSystem.Api.Authorization;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Application.Common.Models;
using ReservationSystem.Application.Services.Dtos;

namespace ReservationSystem.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(IServiceService serviceService) : ControllerBase
{
    /// <summary>Lists services (paged, filtered, sorted). Public; defaults to active only.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<ServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceDto>>> GetAll(
        [FromQuery] ServiceQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await serviceService.GetAllAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single service by id. Public.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await serviceService.GetByIdAsync(id, cancellationToken);
        return Ok(service);
    }

    /// <summary>Creates a new service. Requires BusinessOwner or Admin.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageServices)]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceDto>> Create(
        CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await serviceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing service. Requires BusinessOwner or Admin.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageServices)]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> Update(
        Guid id,
        UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await serviceService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Soft-deletes a service. Requires BusinessOwner or Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageServices)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await serviceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
