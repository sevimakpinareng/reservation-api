using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReservationSystem.Infrastructure.Persistence;

namespace ReservationSystem.Infrastructure;

/// <summary>
/// Registration entry point for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core <see cref="AppDbContext"/> backed by PostgreSQL.
    /// The connection string is read from configuration key
    /// <c>ConnectionStrings:DefaultConnection</c>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. " +
                "Set it via user-secrets, environment variables, or appsettings.Development.json.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
