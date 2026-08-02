using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Persistence.Numbering;

/// <summary>
/// Allocates the next number in a series (ADR-0022 §6). It replaced <c>nextval</c> on five
/// database-global sequences, and it changed two things at once.
///
/// <b>Scope.</b> The counter belongs to a tenant — and, for documents, to a branch — so the second
/// clinic's first invoice is <c>PUR-000001</c> and not <c>PUR-000002</c> (§12.12). The format is
/// unchanged: same prefixes, same six-digit padding, and with one seeded branch the output is
/// byte-identical to what the sequences produced.
///
/// <b>Gaplessness (owner ruling, 2026-08-02).</b> <c>nextval</c> does not roll back, so a failed
/// save used to burn a number permanently. A counter row does roll back with the transaction that
/// touched it. This is only achievable because no document has a delete or cancel path
/// (DEC-INV-037), so the numbers that exist are exactly the documents that exist.
///
/// <b>Therefore this refuses to run outside a transaction.</b> Allocating first and inserting
/// afterwards would leave the increment committed on its own, which is precisely the gap
/// <c>nextval</c> left; requiring the caller's transaction makes the guarantee structural instead
/// of a convention someone has to remember.
///
/// The cost is that two simultaneous creations in the same series serialize on one row until the
/// first commits. At clinic volume that is irrelevant, and ADR-0022 §6 accepts it on the record.
/// </summary>
public sealed class DocumentNumbers(VetFlowDbContext dbContext, ITenantContext tenantContext)
{
    /// <summary>
    /// One statement: create the counter on first use, otherwise increment it, and return the new
    /// value. <c>ON CONFLICT DO UPDATE</c> holds the row lock until the transaction ends, so two
    /// concurrent callers get consecutive numbers rather than the same one — and a rollback undoes
    /// the increment with the document it was for.
    ///
    /// The insert branch is what makes a new branch or a new tenant need no provisioning step: the
    /// first document it ever creates brings its counter into existence (§12.16 — onboarding stays
    /// three inserts).
    /// </summary>
    private const string AllocateSql = """
        INSERT INTO document_counters (tenant_id, scope_id, series, last_value)
        VALUES (@tenant, @scope, @series, 1)
        ON CONFLICT (tenant_id, scope_id, series)
        DO UPDATE SET last_value = document_counters.last_value + 1
        RETURNING last_value
        """;

    public async Task<long> NextAsync(DocumentSeries series, CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                $"The {series.Code()} number must be allocated inside the transaction that persists the " +
                "document. Allocating outside one would commit the increment on its own and reopen the " +
                "gap ADR-0022 §6 removed.");

        var scopeId = series.IsBranchScoped() ? tenantContext.BranchId : tenantContext.TenantId;

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = AllocateSql;
        command.Transaction = transaction.GetDbTransaction();

        Bind(command, "tenant", tenantContext.TenantId);
        Bind(command, "scope", scopeId);
        Bind(command, "series", series.Code());

        return (long)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Allocating a {series.Code()} number returned nothing."));
    }

    private static void Bind(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
