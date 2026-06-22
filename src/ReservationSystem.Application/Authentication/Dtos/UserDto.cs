using ReservationSystem.Domain.Entities;

namespace ReservationSystem.Application.Authentication.Dtos;

/// <summary>Public-facing projection of a <see cref="User"/> (no secrets).</summary>
public record UserDto(Guid Id, string Email, string FullName, string Role)
{
    public static UserDto FromEntity(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString());
}
