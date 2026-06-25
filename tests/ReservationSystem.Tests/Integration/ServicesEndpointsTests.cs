using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ReservationSystem.Application.Common.Models;
using ReservationSystem.Application.Services.Dtos;
using ReservationSystem.Domain.Enums;
using ReservationSystem.Tests.Integration.Infrastructure;
using Xunit;

namespace ReservationSystem.Tests.Integration;

public class ServicesEndpointsTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_IsPublic_AndReturnsOk()
    {
        var response = await CreateClient().GetAsync("/api/services");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsForbidden()
    {
        var customer = await CreateClientForAsync("cust@example.com");

        var response = await customer.PostAsJsonAsync("/api/services",
            new { name = "Haircut", description = (string?)null, durationMinutes = 30, price = 25m });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Anonymous_ReturnsUnauthorized()
    {
        var response = await CreateClient().PostAsJsonAsync("/api/services",
            new { name = "Haircut", durationMinutes = 30, price = 25m });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsBusinessOwner_ReturnsCreatedWithLocation()
    {
        var owner = await CreateClientForAsync("owner@example.com", UserRole.BusinessOwner);

        var response = await owner.PostAsJsonAsync("/api/services",
            new { name = "Haircut", description = "Basic", durationMinutes = 30, price = 25m });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = (await response.Content.ReadFromJsonAsync<ServiceDto>(Json))!;
        created.Name.Should().Be("Haircut");
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_IsSoft_AndSubsequentGetReturnsNotFound()
    {
        var owner = await CreateClientForAsync("owner2@example.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, "Massage");

        var delete = await owner.DeleteAsync($"/api/services/{service.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await CreateClient().GetAsync($"/api/services/{service.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_SupportsSearchSortingAndPageSizeClamp()
    {
        var owner = await CreateClientForAsync("owner3@example.com", UserRole.BusinessOwner);
        await CreateServiceAsync(owner, "Haircut", price: 25m);
        await CreateServiceAsync(owner, "Hair Coloring", price: 80m);
        await CreateServiceAsync(owner, "Massage", price: 50m);

        // Search by name (case-insensitive) matches the two "hair" services.
        var search = await CreateClient().GetFromJsonAsync<PagedResult<ServiceDto>>("/api/services?search=hair", Json);
        search!.TotalCount.Should().Be(2);

        // Sort by price descending.
        var sorted = await CreateClient().GetFromJsonAsync<PagedResult<ServiceDto>>(
            "/api/services?sortBy=Price&sortDescending=true", Json);
        sorted!.Items.Select(s => s.Price).Should().BeInDescendingOrder();

        // Page size is clamped to the max (100).
        var clamped = await CreateClient().GetFromJsonAsync<PagedResult<ServiceDto>>("/api/services?pageSize=9999", Json);
        clamped!.PageSize.Should().Be(100);
    }
}
