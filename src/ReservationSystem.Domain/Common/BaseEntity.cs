namespace ReservationSystem.Domain.Common;

/// <summary>
/// Base type for all persistent domain entities. Provides the identity and
/// auditing fields that every aggregate shares, plus a soft-delete marker.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp set when the entity is first persisted.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the most recent modification.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft-delete flag. Deleted rows remain in the database and are filtered
    /// out by a global query filter in the DbContext.
    /// </summary>
    public bool IsDeleted { get; set; }
}
