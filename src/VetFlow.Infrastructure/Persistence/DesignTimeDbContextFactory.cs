using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for `dotnet ef` only. Migrations are generated from the
/// model and never connect, so a placeholder connection string suffices — the
/// runtime path still refuses to boot without real configuration (STD-BE-048).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VetFlowDbContext>
{
    public VetFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VetFlowDbContext>()
            .UseNpgsql("Host=design-time;Database=design-time;Username=design-time;Password=design-time")
            .Options;
        return new VetFlowDbContext(options, new DesignTimeTenantContext());
    }

    /// <summary>
    /// The scope stand-in used while building the model for a migration. It reports a fixed pair
    /// of ids, which is safe <b>only</b> here: `dotnet ef` never opens a connection and never
    /// executes a filtered query, so these values reach no data. They exist because the query
    /// filters are part of the model and the model must be constructible without a request.
    /// This type is design-time only and is not registered in any container.
    /// </summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId { get; } = Guid.Empty;

        public Guid BranchId { get; } = Guid.Empty;

        public bool IsResolved => false;
    }
}
