using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReservationSystem.Application.Authentication.Dtos;
using ReservationSystem.Application.Services.Dtos;
using ReservationSystem.Domain.Enums;
using Xunit;

namespace ReservationSystem.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for HTTP integration tests. Resets the database before each test
/// and provides helpers for creating authenticated clients.
/// </summary>
[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    /// <summary>Shared JSON options matching the API (camelCase + string enums).</summary>
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>An unauthenticated client.</summary>
    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Registers a user (promoting to <paramref name="role"/> if needed), logs in,
    /// and returns a client with the Bearer token applied.
    /// </summary>
    protected async Task<HttpClient> CreateClientForAsync(string email, UserRole role = UserRole.Customer)
    {
        var client = Factory.CreateClient();
        await RegisterAsync(client, email);

        if (role != UserRole.Customer)
        {
            await Factory.SetUserRoleAsync(email, role);
        }

        var auth = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    protected static async Task<AuthResponse> RegisterAsync(
        HttpClient client, string email, string password = "Str0ng!Pass", string fullName = "Test User")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password, fullName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    protected static async Task<AuthResponse> LoginAsync(
        HttpClient client, string email, string password = "Str0ng!Pass")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    /// <summary>Creates an active service via a staff client and returns it.</summary>
    protected static async Task<ServiceDto> CreateServiceAsync(
        HttpClient staffClient, string name = "Haircut", int durationMinutes = 60, decimal price = 25m)
    {
        var response = await staffClient.PostAsJsonAsync("/api/services",
            new { name, description = (string?)null, durationMinutes, price });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceDto>(Json))!;
    }
}
