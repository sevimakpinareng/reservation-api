using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ReservationSystem.Domain.Entities;

namespace ReservationSystem.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence layer exposed to the Application layer.
/// Implemented by the Infrastructure <c>AppDbContext</c> so application services
/// can query and persist without depending on Infrastructure directly.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Service> Services { get; }

    DbSet<Appointment> Appointments { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins a database transaction (used to make booking atomic).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
