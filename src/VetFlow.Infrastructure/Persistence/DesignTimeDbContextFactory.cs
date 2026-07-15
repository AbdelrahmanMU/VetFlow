using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
        return new VetFlowDbContext(options);
    }
}
