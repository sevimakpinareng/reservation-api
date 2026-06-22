using ReservationSystem.Domain.Common;

namespace ReservationSystem.Domain.Entities;

/// <summary>
/// A long-lived refresh token issued to a <see cref="User"/>. Refresh tokens are
/// rotated on use: when one is exchanged for a new token pair, the old token is
/// revoked. A token is usable only while it is neither expired nor revoked.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="User"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Navigation to the owning user.</summary>
    public User? User { get; set; }

    /// <summary>The opaque token value presented by the client.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC instant after which the token can no longer be used.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC instant the token was revoked, if it has been.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Whether the token has been explicitly revoked (e.g. rotated or on logout).</summary>
    public bool IsRevoked { get; set; }

    /// <summary>True while the token is neither revoked nor past its expiry.</summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
