using Microsoft.EntityFrameworkCore;
using ReservationSystem.Infrastructure.Persistence;

namespace ReservationSystem.Api.Extensions;

/// <summary>
/// Optional, opt-in database migration on startup. Disabled by default so the
/// normal run/test behaviour is unchanged; enabled (via <c>Database:MigrateOnStartup</c>)
/// for the Docker Compose demo so <c>docker compose up</c> works out of the box.
/// In a real deployment, prefer running migrations as a separate release step.
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", false))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
