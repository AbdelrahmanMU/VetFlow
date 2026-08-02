using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Scope-owned, gapless document numbering (ADR-0022 §6, §12.12).
    ///
    /// The five database-global sequences are dropped and replaced by one counter row per
    /// (tenant, scope, series). Two things change and one does not: numbers now start at one for
    /// every clinic and every branch, a failed save no longer burns a number — and the <b>format
    /// is byte-identical</b>, same prefixes and same six-digit padding.
    ///
    /// Dropping the sequences is safe precisely because this lands before the Pilot's first real
    /// entry: all five are verified never called, so nothing depends on their state. After that
    /// point this would have been a migration of accounting series a bookkeeper can see.
    /// </summary>
    public partial class ScopeOwnedDocumentNumbering : Migration
    {
        private static readonly string[] RetiredSequences =
        [
            "product_internal_code_seq",
            "purchase_invoice_number_seq",
            "purchase_return_number_seq",
            "sales_invoice_number_seq",
            "sales_return_number_seq",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "document_counters",
                columns: table => new
                {
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    series = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_counters", x => new { x.tenant_id, x.scope_id, x.series });
                });

            // The counter carries a tenant discriminator, so it is protected like every other
            // tenant-owned table (ADR-0022 §8.2). An unprotected counter would let one clinic's
            // series be read — or advanced — from another's session.
            migrationBuilder.Sql("""
                ALTER TABLE document_counters ENABLE ROW LEVEL SECURITY;
                ALTER TABLE document_counters FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON document_counters
                    USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                    WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
                """);

            foreach (var sequence in RetiredSequences)
            {
                migrationBuilder.Sql($"DROP SEQUENCE IF EXISTS {sequence};");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            foreach (var sequence in RetiredSequences)
            {
                migrationBuilder.Sql(
                    $"CREATE SEQUENCE IF NOT EXISTS {sequence} AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");
            }

            migrationBuilder.DropTable(
                name: "document_counters");
        }
    }
}
