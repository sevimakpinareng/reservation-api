using Microsoft.EntityFrameworkCore;
using ReservationSystem.Application.Common.Exceptions;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Application.Common.Models;
using ReservationSystem.Application.Services.Dtos;
using ReservationSystem.Domain.Entities;

namespace ReservationSystem.Application.Services.Services;

/// <summary>
/// Service management use cases. Soft-deleted services are excluded automatically
/// by the global query filter configured on the database context.
/// </summary>
public class ServiceService(IApplicationDbContext db) : IServiceService
{
    public async Task<PagedResult<ServiceDto>> GetAllAsync(
        ServiceQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var services = db.Services.AsNoTracking();

        // Active filter: when unspecified, show only active services.
        services = query.IsActive is { } isActive
            ? services.Where(s => s.IsActive == isActive)
            : services.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Provider-agnostic case-insensitive match (translated to SQL LOWER(...) LIKE).
            var search = query.Search.Trim().ToLower();
            services = services.Where(s => s.Name.ToLower().Contains(search));
        }

        services = ApplySorting(services, query.SortBy, query.SortDescending);

        var totalCount = await services.CountAsync(cancellationToken);

        var items = await services
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => ServiceDto.FromEntity(s))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ServiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return service is null
            ? throw NotFound(id)
            : ServiceDto.FromEntity(service);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = new Service
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            IsActive = true,
        };

        db.Services.Add(service);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceDto.FromEntity(service);
    }

    public async Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw NotFound(id);

        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return ServiceDto.FromEntity(service);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw NotFound(id);

        // Soft delete: keep the row, flag it. The global query filter hides it.
        service.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Service> ApplySorting(
        IQueryable<Service> services,
        ServiceSortBy sortBy,
        bool descending) => (sortBy, descending) switch
    {
        (ServiceSortBy.Name, false) => services.OrderBy(s => s.Name),
        (ServiceSortBy.Name, true) => services.OrderByDescending(s => s.Name),
        (ServiceSortBy.Price, false) => services.OrderBy(s => s.Price),
        (ServiceSortBy.Price, true) => services.OrderByDescending(s => s.Price),
        (_, true) => services.OrderByDescending(s => s.CreatedAt),
        (_, false) => services.OrderBy(s => s.CreatedAt),
    };

    private static NotFoundException NotFound(Guid id) =>
        new($"Service with id '{id}' was not found.");
}
