namespace ReservationSystem.Application.Services.Validators;

/// <summary>
/// Shared validation bounds for service requests. Lengths mirror the EF Core
/// column constraints configured in the Infrastructure layer.
/// </summary>
public static class ServiceValidationRules
{
    public const int NameMaxLength = 200;

    public const int DescriptionMaxLength = 2000;

    /// <summary>Upper bound for a service duration (24 hours).</summary>
    public const int MaxDurationMinutes = 24 * 60;
}
