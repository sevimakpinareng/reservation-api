using FluentAssertions;
using Npgsql;
using ReservationSystem.Api.Extensions;
using Xunit;

namespace ReservationSystem.Tests.Unit;

public class ConnectionStringConverterTests
{
    [Fact]
    public void FromDatabaseUrl_ParsesAllParts_AndRequiresSsl()
    {
        var url = "postgresql://app_user:p%40ssw0rd@db.frankfurt-postgres.render.com:5432/reservationdb";

        var result = ConnectionStringConverter.FromDatabaseUrl(url);

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.Host.Should().Be("db.frankfurt-postgres.render.com");
        parsed.Port.Should().Be(5432);
        parsed.Database.Should().Be("reservationdb");
        parsed.Username.Should().Be("app_user");
        parsed.Password.Should().Be("p@ssw0rd"); // URL-decoded
        parsed.SslMode.Should().Be(SslMode.Require);
    }

    [Fact]
    public void FromDatabaseUrl_DefaultsPort_WhenMissing()
    {
        var result = ConnectionStringConverter.FromDatabaseUrl("postgresql://u:pw@somehost/db");

        new NpgsqlConnectionStringBuilder(result).Port.Should().Be(5432);
    }
}
