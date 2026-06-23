using ReservationSystem.Domain.Entities;

namespace ReservationSystem.Application.Services.Dtos;

/// <summary>Public-facing projection of a <see cref="Service"/>.</summary>
public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt)
{
    public static ServiceDto FromEntity(Service service) => new(
        service.Id,
        service.Name,
        service.Description,
        service.DurationMinutes,
        service.Price,
        service.IsActive,
        service.CreatedAt);
}
