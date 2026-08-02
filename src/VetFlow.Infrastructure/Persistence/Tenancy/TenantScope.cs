using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence.Tenancy;

/// <summary>
/// The scope machinery behind ADR-0022: how a business entity declares which tenant — and, where
/// it applies, which branch — it belongs to, and how that declaration becomes an automatic filter
/// on every read.
///
/// <b>The discriminators are shadow properties, deliberately.</b> A business entity <i>belongs
/// to</i> a tenant; it does not <i>reason about</i> one. Putting <c>TenantId</c> on the domain
/// classes would push a persistence concern into the domain and force every constructor and call
/// site to carry it, which is exactly the "no fixed tenant identifier in business logic" failure
/// ADR-0022 §12.6 rules out. The Organization entities themselves keep real properties, because
/// for them the tenant <i>is</i> the subject. This mirrors the existing
/// <see cref="SearchableText"/> shadow-property precedent in this codebase.
///
/// <b>Every entity must declare a scope</b> — <see cref="ScopedToTenant{T}"/>,
/// <see cref="ScopedToBranch{T}"/> or <see cref="PlatformGlobal{T}"/>. An entity that declares
/// none is a defect and fails an architecture test, not a silent hole (ADR-0022 §12.1/§12.7).
/// </summary>
public static class TenantScope
{
    /// <summary>The shadow property carrying the owning tenant (BR-ORG-001).</summary>
    public const string TenantIdProperty = "TenantId";

    /// <summary>The shadow property carrying the owning branch (BR-ORG-002).</summary>
    public const string BranchIdProperty = "BranchId";

    /// <summary>Annotation key recording an entity's declared scope, read by the filter pass and by tests.</summary>
    public const string ScopeAnnotation = "VetFlow:TenantScope";

    public const string GlobalScope = "Global";
    public const string TenantScopeName = "Tenant";
    public const string BranchScopeName = "Branch";

    /// <summary>
    /// Reference vocabulary shared by every tenant and written by none — units and product
    /// natures (BR-ORG-001, DEC-ORG-004). Declared explicitly so that "has no discriminator" is
    /// always a decision on the record, never an omission.
    /// </summary>
    public static EntityTypeBuilder<T> PlatformGlobal<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasAnnotation(ScopeAnnotation, GlobalScope);
        return builder;
    }

    /// <summary>
    /// Tenant-owned data that is shared across the tenant's branches — the catalog
    /// (DEC-ORG-006). Gains a required tenant discriminator and a tenant read filter.
    /// </summary>
    public static EntityTypeBuilder<T> ScopedToTenant<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property<Guid>(TenantIdProperty).IsRequired();
        builder.HasAnnotation(ScopeAnnotation, TenantScopeName);
        return builder;
    }

    /// <summary>
    /// Data that belongs to one branch: documents, batches, movements and on-hand balances
    /// (BR-ORG-002). Gains both discriminators and a tenant+branch read filter.
    ///
    /// The branch predicate is applied now, while there is exactly one branch and it therefore
    /// changes nothing, precisely so that opening a second branch does not silently widen every
    /// existing read.
    /// </summary>
    public static EntityTypeBuilder<T> ScopedToBranch<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property<Guid>(TenantIdProperty).IsRequired();
        builder.Property<Guid>(BranchIdProperty).IsRequired();
        builder.HasAnnotation(ScopeAnnotation, BranchScopeName);
        return builder;
    }

    /// <summary>
    /// Applies the automatic read filter to every scoped entity (REQ-ORG-006, AC-ORG-004).
    /// Called once, after all entity configurations, so no configuration can forget it.
    ///
    /// <b><paramref name="tenantContext"/> must be a singleton that resolves per request.</b> EF
    /// caches the model — and this filter with it — for the lifetime of the process, so the
    /// instance captured here is captured <i>once</i>. Capturing a scoped, per-request context
    /// would pin the first request's tenant into the cached model and serve it to every later
    /// request: a total cross-tenant leak that no test of a single request would reveal. The
    /// registered implementation is therefore a singleton reading the ambient scope or the
    /// current principal's claims on each property access.
    /// </summary>
    public static void ApplyReadFilters(ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(tenantContext);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var scope = entityType.FindAnnotation(ScopeAnnotation)?.Value as string;
            if (scope is null || string.Equals(scope, GlobalScope, StringComparison.Ordinal))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            Expression predicate = ScopeEquals(parameter, TenantIdProperty, tenantContext, nameof(ITenantContext.TenantId));

            if (string.Equals(scope, BranchScopeName, StringComparison.Ordinal))
            {
                predicate = Expression.AndAlso(
                    predicate,
                    ScopeEquals(parameter, BranchIdProperty, tenantContext, nameof(ITenantContext.BranchId)));
            }

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(predicate, parameter));
        }
    }

    /// <summary>Reads an entity's declared scope. Null means the entity never declared one.</summary>
    public static string? ReadScope(IReadOnlyEntityType entityType) =>
        entityType?.FindAnnotation(ScopeAnnotation)?.Value as string;

    /// <summary>
    /// Builds <c>EF.Property&lt;Guid&gt;(entity, name) == tenantContext.Member</c>. The right-hand
    /// side is a member access on the singleton, so EF evaluates it per query rather than baking
    /// a value into the cached model.
    /// </summary>
    private static Expression ScopeEquals(
        ParameterExpression parameter,
        string shadowProperty,
        ITenantContext tenantContext,
        string contextMember)
    {
        var efProperty = typeof(EF)
            .GetMethod(nameof(EF.Property))!
            .MakeGenericMethod(typeof(Guid));

        var left = Expression.Call(null, efProperty, parameter, Expression.Constant(shadowProperty));
        var right = Expression.Property(Expression.Constant(tenantContext, typeof(ITenantContext)), contextMember);

        return Expression.Equal(left, right);
    }
}
