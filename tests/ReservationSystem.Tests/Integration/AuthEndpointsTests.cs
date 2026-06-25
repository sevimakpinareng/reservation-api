using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ReservationSystem.Application.Authentication.Dtos;
using ReservationSystem.Tests.Integration.Infrastructure;
using Xunit;

namespace ReservationSystem.Tests.Integration;

public class AuthEndpointsTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_ReturnsTokens_AndDefaultsToCustomer()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "alice@example.com", password = "Str0ng!Pass", fullName = "Alice" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        auth.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
        auth.User.Role.Should().Be("Customer");
        auth.User.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = CreateClient();
        await RegisterAsync(client, "dupe@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "dupe@example.com", password = "Str0ng!Pass", fullName = "Dupe" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "weak@example.com", password = "weak", fullName = "Weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();
        await RegisterAsync(client, "bob@example.com");

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "bob@example.com", password = "WrongPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfile()
    {
        var client = await CreateClientForAsync("carol@example.com");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = (await response.Content.ReadFromJsonAsync<UserDto>(Json))!;
        me.Email.Should().Be("carol@example.com");
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldRefreshIsRejected()
    {
        var client = CreateClient();
        var auth = await RegisterAsync(client, "dan@example.com");

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = (await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        rotated.RefreshToken.Should().NotBe(auth.RefreshToken);

        // The original refresh token has been revoked by rotation.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorized()
    {
        var client = await CreateClientForAsync("erin@example.com");
        var auth = await LoginAsync(client, "erin@example.com");

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = auth.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
