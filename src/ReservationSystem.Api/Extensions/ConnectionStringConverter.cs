using Npgsql;

namespace ReservationSystem.Api.Extensions;

/// <summary>
/// Converts a platform-style database URL (e.g. Render's
/// <c>postgresql://user:pass@host:port/db</c>) into the key/value connection
/// string that Npgsql expects. Used only when a <c>DATABASE_URL</c> is supplied.
/// </summary>
public static class ConnectionStringConverter
{
    public static string FromDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            // In Npgsql 10, SslMode.Require encrypts without validating the server
            // certificate — the right choice for managed providers like Render.
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
