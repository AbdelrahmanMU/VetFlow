using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// <b>Why every table in this file is <see cref="TenantScope.PlatformGlobal{T}"/>, when
/// ADR-0022 §12.1 says every business row carries a tenant.</b>
///
/// These are not business rows — they are the rows that <i>define</i> tenancy, and they must be
/// readable <b>before</b> a tenant is known. Sign-in looks a user up by phone number and then
/// reads the membership to discover which tenant and branch to put in the token. If
/// <see cref="Membership"/> carried a tenant filter, that filter would ask the tenant context for
/// a tenant that sign-in has not resolved yet — the row that would answer the question could
/// never be read. The same applies to <see cref="Tenant"/> and <see cref="Branch"/>, which the
/// seeder creates before any tenant exists at all.
///
/// The isolation they lose from the filter, they regain from the login path: a user with no
/// membership simply fails to sign in, with the same unified message as a wrong password
/// (BR-IDN-007, BR-IDN-003). Nothing here is ever returned by a business endpoint.
///
/// <see cref="Membership"/> and <see cref="Branch"/> also hold real <c>TenantId</c> properties
/// rather than shadow ones — for them the tenant is the subject, not a label — which is a second,
/// independent reason they must not be given the shadow discriminator.
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.PlatformGlobal();
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).HasMaxLength(300).IsRequired();
        builder.Property(tenant => tenant.TimeZone).HasMaxLength(100).IsRequired();

        // The principal side of every composite tenant foreign key (ADR-0022 §12.2).
        builder.HasIndex(tenant => tenant.Id).IsUnique();
    }
}

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.PlatformGlobal();
        builder.HasKey(branch => branch.Id);
        builder.Property(branch => branch.Id).ValueGeneratedNever();
        builder.Property(branch => branch.TenantId).IsRequired();
        builder.Property(branch => branch.Name).HasMaxLength(300).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(branch => branch.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(branch => branch.TenantId);
    }
}

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.PlatformGlobal();
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).ValueGeneratedNever();
        builder.Property(membership => membership.TenantId).IsRequired();
        builder.Property(membership => membership.BranchId).IsRequired();
        builder.Property(membership => membership.UserId).IsRequired();
        builder.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(membership => membership.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(membership => membership.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(membership => membership.UserId).OnDelete(DeleteBehavior.Restrict);

        // One membership per user per tenant (organization/business-rules.md, Validations).
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
        builder.HasIndex(membership => membership.UserId);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.PlatformGlobal();
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();

        // Unique across the entire system, not per tenant — owner ruling OQ-IDN-4,
        // BR-IDN-001, ADR-0022 §12.14. Tenant-scoped uniqueness would require knowing the
        // tenant before knowing the user, which is impossible at sign-in without letting the
        // client name a tenant before it has authenticated.
        builder.HasIndex(user => user.PhoneNumber).IsUnique();
    }
}
