using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence.Attribution;

/// <summary>
/// Stamps the authenticated performer on every attributed row (REQ-IDN-008, AC-IDN-011,
/// BR-INV-066 as amended). The write-side twin of the tenant stamp: no caller passes it, and no
/// caller can forget it.
///
/// <b>An unauthenticated write is refused, not attributed to nobody.</b> The rule is that every
/// operation has an authenticated performer; a zero-guid fallback would make the ledger say a
/// movement had one when it did not — and an append-only record cannot be corrected afterwards.
///
/// Seeding is the one legitimate writer with no principal, and it writes no attributed rows: the
/// organization seed creates a tenant, a branch, a user and a membership, and the development
/// seed creates purchase invoices. Neither is a movement.
/// </summary>
public sealed class ActorStampInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not EntityState.Added
                || entry.Metadata.FindAnnotation(PerformedBy.Annotation) is null)
            {
                continue;
            }

            if (!currentUser.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    $"Cannot persist '{entry.Metadata.DisplayName()}': no authenticated user. " +
                    "Every operation is attributed to the signed-in user (BR-INV-066, REQ-IDN-008).");
            }

            entry.Property(PerformedBy.UserIdProperty).CurrentValue = currentUser.UserId;
        }
    }
}
