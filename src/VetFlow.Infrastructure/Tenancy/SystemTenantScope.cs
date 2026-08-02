namespace VetFlow.Infrastructure.Tenancy;

/// <summary>
/// An explicitly established organizational scope for work that runs <b>outside a request</b>:
/// database seeding, start-up bootstrap, and integration tests.
///
/// <b>This is not a way to choose a tenant in business logic.</b> Business code never touches it;
/// it depends on <c>ITenantContext</c>, which resolves from authenticated claims (BR-IDN-004,
/// ADR-0022 §12.5). This type exists because seeding legitimately runs before any user exists —
/// the very code that <i>creates</i> the first tenant cannot resolve one from a token — and
/// because integration tests must be able to assert isolation by acting as two different tenants.
///
/// An architecture test pins that only Infrastructure and tests reference it, so it cannot drift
/// into an endpoint or a handler and become the fixed tenant identifier §12.6 prohibits.
///
/// Ambient state is <see cref="AsyncLocal{T}"/> and therefore flows with the async operation and
/// unwinds with it; nested scopes restore their parent on dispose.
/// </summary>
public static class SystemTenantScope
{
    private static readonly AsyncLocal<Scope?> Current = new();

    /// <summary>The scope in force, or null when none has been established.</summary>
    public static (Guid TenantId, Guid BranchId)? CurrentScope =>
        Current.Value is { } scope ? (scope.TenantId, scope.BranchId) : null;

    /// <summary>
    /// Establishes a scope until the returned handle is disposed. Seeding and tests only.
    /// </summary>
    public static IDisposable Begin(Guid tenantId, Guid branchId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(branchId, Guid.Empty);

        var previous = Current.Value;
        Current.Value = new Scope(tenantId, branchId);
        return new Handle(previous);
    }

    private sealed record Scope(Guid TenantId, Guid BranchId);

    private sealed class Handle(Scope? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Current.Value = previous;
        }
    }
}
