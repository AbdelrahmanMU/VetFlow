using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Infrastructure.Persistence.Numbering;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// The counter table behind ADR-0022 §6. It is tenant-scoped like any other row that belongs to a
/// customer; the branch dimension is carried by <see cref="DocumentCounter.ScopeId"/> rather than
/// the branch discriminator, because a tenant-scoped series (<c>PRD-</c>) has no branch and a
/// nullable column cannot be part of a primary key.
/// </summary>
public sealed class DocumentCounterConfiguration : IEntityTypeConfiguration<DocumentCounter>
{
    public void Configure(EntityTypeBuilder<DocumentCounter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Named explicitly, and plural like every other table: the global snake_case convention
        // derives a table name from the DbSet, and this entity deliberately has none — nothing
        // reads or writes it through EF.
        builder.ToTable("document_counters");

        builder.ScopedToTenant();

        // The key is the allocation's conflict target: one row per tenant, scope and series, and
        // the row lock that makes concurrent creations consecutive rather than colliding.
        builder.HasKey(TenantScope.TenantIdProperty, nameof(DocumentCounter.ScopeId), nameof(DocumentCounter.Series));

        builder.Property(counter => counter.Series).HasMaxLength(8).IsRequired();
        builder.Property(counter => counter.LastValue).IsRequired();
    }
}
