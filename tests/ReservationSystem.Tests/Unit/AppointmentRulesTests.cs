using FluentAssertions;
using ReservationSystem.Domain.Appointments;
using ReservationSystem.Domain.Enums;
using Xunit;

namespace ReservationSystem.Tests.Unit;

public class AppointmentRulesTests
{
    private static readonly DateTime Base = new(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeEndTime_AddsDurationMinutes()
    {
        AppointmentRules.ComputeEndTime(Base, 90).Should().Be(Base.AddMinutes(90));
    }

    [Theory]
    // existing 10:00-11:00; new slot vs it
    [InlineData(0, 60, true)]    // identical -> overlap
    [InlineData(30, 90, true)]   // starts inside -> overlap
    [InlineData(-30, 30, true)]  // ends inside -> overlap
    [InlineData(15, 45, true)]   // fully inside -> overlap
    [InlineData(-30, 120, true)] // fully covers -> overlap
    [InlineData(60, 120, false)] // starts exactly when existing ends -> NO overlap (half-open)
    [InlineData(-60, 0, false)]  // ends exactly when existing starts -> NO overlap (half-open)
    [InlineData(120, 180, false)]// completely after -> no overlap
    public void Overlaps_HalfOpenInterval_BehavesCorrectly(int newStartOffset, int newEndOffset, bool expected)
    {
        var existingStart = Base;                 // 10:00
        var existingEnd = Base.AddMinutes(60);    // 11:00
        var newStart = Base.AddMinutes(newStartOffset);
        var newEnd = Base.AddMinutes(newEndOffset);

        AppointmentRules.Overlaps(newStart, newEnd, existingStart, existingEnd).Should().Be(expected);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending, true)]
    [InlineData(AppointmentStatus.Confirmed, false)]
    [InlineData(AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Cancelled, false)]
    public void CanConfirm_OnlyFromPending(AppointmentStatus status, bool expected) =>
        AppointmentRules.CanConfirm(status).Should().Be(expected);

    [Theory]
    [InlineData(AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Pending, false)]
    [InlineData(AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Cancelled, false)]
    public void CanComplete_OnlyFromConfirmed(AppointmentStatus status, bool expected) =>
        AppointmentRules.CanComplete(status).Should().Be(expected);

    [Theory]
    [InlineData(AppointmentStatus.Pending, true)]
    [InlineData(AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Cancelled, false)]
    public void CanCancel_FromPendingOrConfirmed(AppointmentStatus status, bool expected) =>
        AppointmentRules.CanCancel(status).Should().Be(expected);
}
