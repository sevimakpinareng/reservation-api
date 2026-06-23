using ReservationSystem.Application.Common.Models;
using ReservationSystem.Application.Services.Dtos;

namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>CRUD use cases for managing bookable services.</summary>
public interface IServiceService
{
    /// <summary>Returns a filtered, sorted, paged list of services.</summary>
    Task<PagedResult<ServiceDto>> GetAllAsync(ServiceQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Returns a single service by id, or throws if it does not exist.</summary>
    Task<ServiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new service.</summary>
    Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing service, or throws if it does not exist.</summary>
    Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a service, or throws if it does not exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
