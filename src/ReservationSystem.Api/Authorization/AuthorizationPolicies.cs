namespace ReservationSystem.Api.Authorization;

/// <summary>Names of the authorization policies registered for the API.</summary>
public static class AuthorizationPolicies
{
    public const string Customer = nameof(Customer);

    public const string BusinessOwner = nameof(BusinessOwner);

    public const string Admin = nameof(Admin);

    /// <summary>Either a BusinessOwner or an Admin may manage services.</summary>
    public const string ManageServices = nameof(ManageServices);
}
