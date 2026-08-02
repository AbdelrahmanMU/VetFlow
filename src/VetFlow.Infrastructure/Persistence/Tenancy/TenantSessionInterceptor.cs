using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Publishes the current tenant to the database session, which is what makes PostgreSQL
/// row-level security — the second of ADR-0022 §8's four mandatory mitigations — able to act.
/// The policies installed by the <c>TenantRowLevelSecurity</c> migration compare each row's
/// <c>tenant_id</c> against <c>current_setting('app.tenant_id')</c>; this interceptor is the only
/// thing that ever sets it.
///
/// <b>Why a second net at all.</b> The EF query filter is the first, and it is good — but it is a
/// property of the model. RLS is a property of the database: it also covers a hand-written query,
/// a future report, a maintenance script and a mistake, none of which the model can reach. §12.7
/// makes it non-negotiable and says the shared-database decision of §2 is void without it.
///
/// <b>It is set on every connection open, including when no scope exists.</b> Connections are
/// pooled: without the unconditional write, a connection returned by a signed-in request would
/// hand its tenant to whatever borrowed it next. When no scope is resolved the value is cleared to
/// the empty string, and the policies — which read it null-safely — then match nothing at all.
/// <b>Fail-closed is deliberate</b>: the failure mode of a missing scope is "sees nothing", never
/// "sees everything".
///
/// The six tables that define tenancy carry no policy (DEC-ORG-009), so sign-in can still read the
/// user and the membership it needs in order to discover a tenant in the first place.
/// </summary>
public sealed class TenantSessionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    /// <summary>The session variable the policies read. Namespaced, as PostgreSQL requires.</summary>
    public const string SessionVariable = "app.tenant_id";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var command = BuildCommand(connection);
        command.ExecuteNonQuery();

        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var command = BuildCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private DbCommand BuildCommand(DbConnection connection)
    {
        // set_config(..., is_local: false) sets it for the session rather than the transaction:
        // reads outside an explicit transaction need it just as much as writes inside one.
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('" + SessionVariable + "', @tenant, false)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";

        // Reading the property is what resolves the scope, so the check comes first: an
        // unresolved context throws by design rather than answering with a default.
        parameter.Value = tenantContext.IsResolved
            ? tenantContext.TenantId.ToString()
            : string.Empty;

        command.Parameters.Add(parameter);
        return command;
    }
}
