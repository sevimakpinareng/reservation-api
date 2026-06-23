namespace ReservationSystem.Application.Common;

/// <summary>Shared pagination bounds applied across paged queries.</summary>
public static class PaginationDefaults
{
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;
}
