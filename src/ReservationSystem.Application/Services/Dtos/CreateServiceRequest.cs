namespace ReservationSystem.Application.Services.Dtos;

/// <summary>Payload for creating a new service.</summary>
public record CreateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price);
