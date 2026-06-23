using ReservationSystem.Application.Common;

namespace ReservationSystem.Application.Services.Dtos;

/// <summary>
/// Query-string parameters for listing services. Page and page size are clamped
/// to safe bounds so a client cannot request an oversized page.
/// </summary>
public class ServiceQueryParameters
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

    /// <summary>Optional case-insensitive substring match on the service name.</summary>
    public string? Search { get; set; }

    /// <summary>Field to sort by. Defaults to creation date.</summary>
    public ServiceSortBy SortBy { get; set; } = ServiceSortBy.CreatedAt;

    /// <summary>Sort descending when true; ascending otherwise.</summary>
    public bool SortDescending { get; set; }

    /// <summary>
    /// Optional active-state filter. When omitted, only active services are
    /// returned (the default public view).
    /// </summary>
    public bool? IsActive { get; set; }
}
