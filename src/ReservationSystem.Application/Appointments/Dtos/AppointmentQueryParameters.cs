using ReservationSystem.Application.Common;
using ReservationSystem.Domain.Enums;

namespace ReservationSystem.Application.Appointments.Dtos;

/// <summary>
/// Query-string parameters for listing appointments. Page and page size are
/// clamped to safe bounds. Customers only ever see their own appointments
/// regardless of these filters.
/// </summary>
public class AppointmentQueryParameters
{
    private int _page = PaginationDefaults.DefaultPage;
    private int _pageSize = PaginationDefaults.DefaultPageSize;

    /// <summary>1-based page number (minimum 1).</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? PaginationDefaults.DefaultPage : value;
    }

    /// <summary>Items per page (1..<see cref="PaginationDefaults.MaxPageSize"/>).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => PaginationDefaults.DefaultPageSize,
            > PaginationDefaults.MaxPageSize => PaginationDefaults.MaxPageSize,
            _ => value,
        };
    }

    /// <summary>Optional status filter.</summary>
    public AppointmentStatus? Status { get; set; }

    /// <summary>Optional service filter.</summary>
    public Guid? ServiceId { get; set; }

    /// <summary>Optional inclusive lower bound on start time (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Optional exclusive upper bound on start time (UTC).</summary>
    public DateTime? To { get; set; }

    /// <summary>Field to sort by. Defaults to start time.</summary>
    public AppointmentSortBy SortBy { get; set; } = AppointmentSortBy.StartTime;

    /// <summary>Sort descending when true; ascending otherwise.</summary>
    public bool SortDescending { get; set; }
}
