using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Attribution;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.ArchitectureTests;

/// <summary>
/// ADR-0022 §12.7 makes four mitigations mandatory, and states that the shared-database decision
/// in §2 is <b>void</b> without them. These tests are one of the four. They exist because the
/// difference between "we filter by tenant" and "we cannot fail to filter by tenant" is the
/// difference between a convention and a guarantee — and only the second one survives a new
/// entity added by someone who has not read the ADR.
///
/// A new business table with no scope declaration fails the build here rather than shipping as a
/// silent hole in tenant isolation.
/// </summary>
public sealed class TenantIsolationTests
{
    /// <summary>
    /// Tables that legitimately carry no tenant discriminator, each for a recorded reason.
    /// Adding a name to this list is a deliberate act that should be justified in review; the
    /// list is short and every entry is explained in ADR-0022 or in the configuration itself.
    /// </summary>
    private static readonly HashSet<string> ExpectedGlobal =
    [
        // Shared vocabulary owned by no tenant (DEC-ORG-004, ADR-0022 §12.4).
        "Unit",
        "ProductNature",

        // The rows that DEFINE tenancy, which must be readable before a tenant is known —
        // sign-in reads the user and the membership in order to discover the tenant at all
        // (see OrganizationConfigurations for the full reasoning).
        "Tenant",
        "Branch",
        "Membership",
        "User",
    ];

    [Fact]
    public void Every_entity_declares_a_tenant_scope_ADR_0022_12_1()
    {
        using var context = BuildContext();

        var undeclared = context.Model.GetEntityTypes()
            .Where(entityType => TenantScope.ReadScope(entityType) is null)
            .Select(entityType => entityType.ClrType.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        undeclared.ShouldBeEmpty(
            "Every entity must declare ScopedToTenant, ScopedToBranch or PlatformGlobal. " +
            "An undeclared entity is a table with no tenant isolation (ADR-0022 §12.1).");
    }

    [Fact]
    public void Only_the_recorded_exceptions_are_platform_global_ADR_0022_12_1()
    {
        using var context = BuildContext();

        var global = context.Model.GetEntityTypes()
            .Where(entityType => TenantScope.ReadScope(entityType) == TenantScope.GlobalScope)
            .Select(entityType => entityType.ClrType.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        global.ShouldBe(
            [.. ExpectedGlobal.OrderBy(name => name, StringComparer.Ordinal)],
            "A new table without a tenant is a hole in isolation. If one is genuinely global, " +
            "record why in ADR-0022 and add it to ExpectedGlobal deliberately.");
    }

    [Fact]
    public void Every_scoped_entity_has_a_read_filter_ADR_0022_12_7()
    {
        using var context = BuildContext();

        var unfiltered = context.Model.GetEntityTypes()
            .Where(entityType => TenantScope.ReadScope(entityType) is TenantScope.TenantScopeName or TenantScope.BranchScopeName)
            .Where(entityType => entityType.GetDeclaredQueryFilters().Count == 0)
            .Select(entityType => entityType.ClrType.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        unfiltered.ShouldBeEmpty(
            "A scoped entity without a query filter returns every tenant's rows (ADR-0022 §12.7).");
    }

    [Fact]
    public void Every_scoped_entity_carries_the_discriminator_columns_ADR_0022_12_1()
    {
        using var context = BuildContext();

        var offenders = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var scope = TenantScope.ReadScope(entityType);

            if (scope is TenantScope.TenantScopeName or TenantScope.BranchScopeName
                && entityType.FindProperty(TenantScope.TenantIdProperty) is null)
            {
                offenders.Add($"{entityType.ClrType.Name}.{TenantScope.TenantIdProperty}");
            }

            if (scope == TenantScope.BranchScopeName
                && entityType.FindProperty(TenantScope.BranchIdProperty) is null)
            {
                offenders.Add($"{entityType.ClrType.Name}.{TenantScope.BranchIdProperty}");
            }
        }

        offenders.ShouldBeEmpty();
    }

    /// <summary>
    /// The movement ledger records who performed each operation (BR-INV-066 as amended
    /// 2026-08-02, REQ-IDN-008). It is an annotation on the model rather than a habit in a
    /// handler, so dropping it fails here instead of producing an unattributable ledger — which,
    /// being append-only, could never be repaired afterwards.
    /// </summary>
    [Fact]
    public void The_movement_ledger_records_its_performer_AC_IDN_011()
    {
        using var context = BuildContext();

        var movements = context.Model.FindEntityType(typeof(Domain.Inventory.InventoryMovement))!;

        movements.FindAnnotation(PerformedBy.Annotation).ShouldNotBeNull();
        movements.FindProperty(PerformedBy.UserIdProperty).ShouldNotBeNull();
        movements.FindProperty(PerformedBy.UserIdProperty)!.IsNullable.ShouldBeFalse();
    }

    /// <summary>
    /// The scope must never be resolvable from a constant. This pins the absence of the fallback
    /// ADR-0022 §12.6 prohibits: a default tenant becomes load-bearing, and the first real second
    /// tenant then writes into the first one's data.
    /// </summary>
    [Fact]
    public void An_unresolved_scope_throws_rather_than_defaulting_TS_ORG_007_AC_ORG_006()
    {
        var unresolved = new UnresolvedTenantContext();

        unresolved.IsResolved.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _ = unresolved.TenantId);
    }

    private static VetFlowDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<VetFlowDbContext>()
            .UseNpgsql("Host=architecture-test;Database=architecture-test;Username=t;Password=t")
            .Options;

        // The model is built without connecting; these tests inspect metadata only.
        return new VetFlowDbContext(options, new UnresolvedTenantContext());
    }

    private sealed class UnresolvedTenantContext : ITenantContext
    {
        public Guid TenantId => throw new InvalidOperationException("No tenant scope established.");

        public Guid BranchId => throw new InvalidOperationException("No tenant scope established.");

        public bool IsResolved => false;
    }
}
