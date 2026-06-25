namespace ReservationSystem.Application.Appointments.Dtos;

/// <summary>
/// Payload for booking an appointment. The end time is computed on the server
/// from the service duration, and the customer is taken from the access token —
/// never from the client.
/// </summary>
public record CreateAppointmentRequest(Guid ServiceId, DateTime StartTime);
