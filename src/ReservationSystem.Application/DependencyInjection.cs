using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReservationSystem.Application.Appointments.Services;
using ReservationSystem.Application.Authentication.Services;
using ReservationSystem.Application.Common;
using ReservationSystem.Application.Common.Interfaces;
using ReservationSystem.Application.Services.Services;

namespace ReservationSystem.Application;

/// <summary>Registration entry point for the Application layer.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
