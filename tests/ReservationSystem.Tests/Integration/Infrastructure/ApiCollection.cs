using Xunit;

namespace ReservationSystem.Tests.Integration.Infrastructure;

/// <summary>
/// Groups all integration tests into a single collection so they share one
/// PostgreSQL container and run sequentially (each test resets the database
/// first, keeping them deterministic and order-independent).
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
