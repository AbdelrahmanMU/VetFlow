using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Stamps the tenant and branch discriminators on every inserted row, so no caller can forget
/// them and no caller can supply them (BR-ORG-003, AC-ORG-005).
///
/// This is the write-side counterpart to <see cref="TenantScope.ApplyReadFilters"/>: reads are
/// filtered automatically, writes are labelled automatically, and neither is a parameter any
/// handler passes. That is what makes "every business operation automatically inherits tenant →
/// branch → user without any manual selection" true by construction rather than by convention.
///
/// <b>An unresolved scope refuses the write.</b> There is no default tenant and no fallback
/// (ADR-0022 §12.6): a fallback would become load-bearing, and the first genuine second tenant
/// would write silently into the first one's data. Failing loudly at the seam is the cheap
/// outcome; a mislabelled row is not repairable, because nothing records what it should have been.
///
/// Existing rows are never re-stamped. The discriminators are immutable once written — a row does
/// not change tenant, and a modification that appeared to do so would be a defect worth surfacing
/// rather than silently correcting.
/// </summary>
public sealed class TenantStampInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
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
            if (entry.State is not EntityState.Added)
            {
                continue;
            }

            var scope = TenantScope.ReadScope(entry.Metadata);
            if (scope is null || string.Equals(scope, TenantScope.GlobalScope, StringComparison.Ordinal))
            {
                continue;
            }

            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    $"Cannot persist '{entry.Metadata.DisplayName()}': no tenant scope is established. " +
                    "Writes never fall back to a default tenant (ADR-0022 §12.6).");
            }

            entry.Property(TenantScope.TenantIdProperty).CurrentValue = tenantContext.TenantId;

            if (string.Equals(scope, TenantScope.BranchScopeName, StringComparison.Ordinal))
            {
                entry.Property(TenantScope.BranchIdProperty).CurrentValue = tenantContext.BranchId;
            }
        }
    }
}
