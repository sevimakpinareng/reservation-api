namespace ReservationSystem.Application.Services.Dtos;

/// <summary>Payload for updating an existing service.</summary>
public record UpdateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive);
