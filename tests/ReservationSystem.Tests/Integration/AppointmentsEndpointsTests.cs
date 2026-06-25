using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ReservationSystem.Application.Appointments.Dtos;
using ReservationSystem.Application.Common.Models;
using ReservationSystem.Application.Services.Dtos;
using ReservationSystem.Domain.Enums;
using ReservationSystem.Tests.Integration.Infrastructure;
using Xunit;

namespace ReservationSystem.Tests.Integration;

public class AppointmentsEndpointsTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private static DateTime FutureStart(int daysAhead = 1) =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddDays(daysAhead).Date.AddHours(10), DateTimeKind.Utc); // 10:00 UTC

    private static Task<HttpResponseMessage> BookAsync(HttpClient client, Guid serviceId, DateTime startUtc) =>
        client.PostAsJsonAsync("/api/appointments", new { serviceId, startTime = startUtc });

    [Fact]
    public async Task Book_ValidRequest_ReturnsCreated_AndComputesEndTimeOnServer()
    {
        var owner = await CreateClientForAsync("owner@appt.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, "Coloring", durationMinutes: 90);
        var customer = await CreateClientForAsync("cust@appt.com");

        var response = await BookAsync(customer, service.Id, FutureStart());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var appt = (await response.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;
        appt.Status.Should().Be(AppointmentStatus.Pending);
        (appt.EndTime - appt.StartTime).TotalMinutes.Should().Be(90); // server computed
        appt.CustomerEmail.Should().Be("cust@appt.com");
    }

    [Fact]
    public async Task Book_OverlappingSlot_ReturnsConflict()
    {
        var owner = await CreateClientForAsync("owner@ov.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, "Coloring", durationMinutes: 60);
        var customer = await CreateClientForAsync("cust@ov.com");
        var start = FutureStart();

        (await BookAsync(customer, service.Id, start)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Overlaps the first booking (starts 30 min into it).
        var overlap = await BookAsync(customer, service.Id, start.AddMinutes(30));
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Book_PastStart_ReturnsBadRequest()
    {
        var owner = await CreateClientForAsync("owner@past.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner);
        var customer = await CreateClientForAsync("cust@past.com");

        var response = await BookAsync(customer, service.Id, DateTime.UtcNow.AddHours(-2));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Book_InactiveService_ReturnsBadRequest()
    {
        var owner = await CreateClientForAsync("owner@inactive.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, "Massage");
        // Deactivate it.
        await owner.PutAsJsonAsync($"/api/services/{service.Id}",
            new { name = service.Name, description = (string?)null, durationMinutes = service.DurationMinutes, price = service.Price, isActive = false });
        var customer = await CreateClientForAsync("cust@inactive.com");

        var response = await BookAsync(customer, service.Id, FutureStart());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Book_NonexistentService_ReturnsNotFound()
    {
        var customer = await CreateClientForAsync("cust@missing.com");

        var response = await BookAsync(customer, Guid.NewGuid(), FutureStart());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConcurrentBookings_SameSlot_ExactlyOneSucceeds()
    {
        var owner = await CreateClientForAsync("owner@race.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, "Coloring", durationMinutes: 60);
        var customer = await CreateClientForAsync("cust@race.com");
        var start = FutureStart();

        // Fire two identical bookings at the same time.
        var first = BookAsync(customer, service.Id, start);
        var second = BookAsync(customer, service.Id, start);
        var responses = await Task.WhenAll(first, second);

        var statuses = responses.Select(r => r.StatusCode).ToList();
        statuses.Count(s => s == HttpStatusCode.Created).Should().Be(1, "exactly one booking may win the slot");
        statuses.Count(s => s == HttpStatusCode.Conflict).Should().Be(1, "the GiST exclusion constraint must reject the loser");
    }

    [Fact]
    public async Task GetById_OtherCustomersAppointment_ReturnsForbidden()
    {
        var owner = await CreateClientForAsync("owner@vis.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner);
        var alice = await CreateClientForAsync("alice@vis.com");
        var bob = await CreateClientForAsync("bob@vis.com");

        var booking = await BookAsync(alice, service.Id, FutureStart());
        var appt = (await booking.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;

        var bobView = await bob.GetAsync($"/api/appointments/{appt.Id}");
        bobView.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_CustomerSeesOnlyOwn_OwnerSeesAll()
    {
        var owner = await CreateClientForAsync("owner@list.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, durationMinutes: 60);
        var alice = await CreateClientForAsync("alice@list.com");
        var bob = await CreateClientForAsync("bob@list.com");

        await BookAsync(alice, service.Id, FutureStart(1));
        await BookAsync(bob, service.Id, FutureStart(2));

        var aliceList = await alice.GetFromJsonAsync<PagedResult<AppointmentDto>>("/api/appointments", Json);
        aliceList!.TotalCount.Should().Be(1);

        var ownerList = await owner.GetFromJsonAsync<PagedResult<AppointmentDto>>("/api/appointments", Json);
        ownerList!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Lifecycle_OwnerConfirmsAndCompletes_CustomerCannotConfirm()
    {
        var owner = await CreateClientForAsync("owner@life.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, durationMinutes: 60);
        var customer = await CreateClientForAsync("cust@life.com");

        var booking = await BookAsync(customer, service.Id, FutureStart());
        var appt = (await booking.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;

        // Customer cannot confirm (policy: staff only).
        (await customer.PostAsync($"/api/appointments/{appt.Id}/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var confirm = await owner.PostAsync($"/api/appointments/{appt.Id}/confirm", null);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
        (await confirm.Content.ReadFromJsonAsync<AppointmentDto>(Json))!.Status.Should().Be(AppointmentStatus.Confirmed);

        var complete = await owner.PostAsync($"/api/appointments/{appt.Id}/complete", null);
        (await complete.Content.ReadFromJsonAsync<AppointmentDto>(Json))!.Status.Should().Be(AppointmentStatus.Completed);

        // Completed -> confirm is an invalid transition.
        (await owner.PostAsync($"/api/appointments/{appt.Id}/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_OwnPending_AsCustomer_ThenSlotCanBeRebooked()
    {
        var owner = await CreateClientForAsync("owner@cancel.com", UserRole.BusinessOwner);
        var service = await CreateServiceAsync(owner, durationMinutes: 60);
        var customer = await CreateClientForAsync("cust@cancel.com");
        var start = FutureStart();

        var booking = await BookAsync(customer, service.Id, start);
        var appt = (await booking.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;

        var cancel = await customer.PostAsync($"/api/appointments/{appt.Id}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        (await cancel.Content.ReadFromJsonAsync<AppointmentDto>(Json))!.Status.Should().Be(AppointmentStatus.Cancelled);

        // Cancelled appointments free the slot.
        var rebook = await BookAsync(customer, service.Id, start);
        rebook.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
