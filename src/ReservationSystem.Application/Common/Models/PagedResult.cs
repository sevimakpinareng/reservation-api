namespace ReservationSystem.Application.Common.Models;

/// <summary>
/// A single page of results plus the paging metadata needed by clients to
/// navigate the full set.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Total number of pages for the current <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
