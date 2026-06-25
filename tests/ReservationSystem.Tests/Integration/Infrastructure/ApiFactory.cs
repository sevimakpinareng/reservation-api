using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservationSystem.Domain.Enums;
using ReservationSystem.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReservationSystem.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real API via <see cref="WebApplicationFactory{TEntryPoint}"/> but
/// points it at a throwaway PostgreSQL 17 container started with Testcontainers.
/// The container starts once per test collection; migrations (including the GiST
/// overlap constraint) are applied so tests run against the real schema.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // The app reads configuration eagerly while building the host (e.g. the JWT
        // secret), before WebApplicationFactory's ConfigureAppConfiguration runs.
        // Environment variables are picked up by CreateBuilder up front, so set them
        // here — after the container is up — before the host is first built.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-tests-signing-secret-key-please-change-0123456789");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ReservationSystem.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "ReservationSystem.Tests");

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>Truncates all tables so each test starts from a clean slate.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE appointments, refresh_tokens, services, users RESTART IDENTITY CASCADE;");
    }

    /// <summary>Promotes a registered user to a role (used to obtain staff accounts).</summary>
    public async Task SetUserRoleAsync(string email, UserRole role)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == email.ToLowerInvariant());
        user.Role = role;
        await context.SaveChangesAsync();
    }
}
