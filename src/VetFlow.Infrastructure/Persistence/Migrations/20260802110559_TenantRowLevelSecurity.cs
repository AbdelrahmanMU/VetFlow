using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// PostgreSQL row-level security — the second of the four mitigations ADR-0022 §8 makes
    /// mandatory, and without which §12.7 declares the shared-database decision void.
    ///
    /// The EF global query filter is the first net and it is a property of the <i>model</i>. This
    /// one is a property of the <i>database</i>: it also covers a hand-written query, a future
    /// report, a maintenance script and a mistake — none of which the model can reach. The two
    /// nets fail independently, which is the entire point of having both.
    ///
    /// <c>FORCE</c> matters as much as <c>ENABLE</c>: the application role owns these tables, and
    /// a table owner is exempt from its own policies unless the table forces them. A superuser is
    /// exempt regardless — which is why the deployed role must not be one (Neon's is not).
    ///
    /// The predicate is null-safe on purpose. A connection with no tenant published to it —
    /// <c>TenantSessionInterceptor</c> clears the variable when no scope is resolved — matches no
    /// row rather than every row. <b>The failure mode of a missing scope is "sees nothing".</b>
    ///
    /// The four tables that define tenancy (tenants, branches, memberships, users) and the shared
    /// vocabulary (units, product_natures) carry no policy, per DEC-ORG-009 and ADR-0022 §12.4:
    /// sign-in reads the membership <i>in order to discover</i> the tenant, so a policy there would
    /// ask the session for the very value the row exists to supply.
    /// </summary>
    public partial class TenantRowLevelSecurity : Migration
    {
        private const string PolicyName = "tenant_isolation";

        /// <summary>
        /// Every table carrying a tenant discriminator (ADR-0022 §4). The branch discriminator is
        /// deliberately not part of the policy: a branch is an organizational scope inside one
        /// customer, and the query filter enforces it, whereas the tenant is the security boundary
        /// — the thing a second net exists to protect.
        /// </summary>
        private static readonly string[] TenantScopedTables =
        [
            "products",
            "product_units",
            "categories",
            "manufacturers",
            "purchase_invoices",
            "purchase_line_items",
            "purchase_returns",
            "purchase_return_lines",
            "sales_invoices",
            "sales_line_items",
            "sales_returns",
            "sales_return_lines",
            "inventory_batches",
            "inventory_movements",
            "product_on_hands",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE {table} FORCE ROW LEVEL SECURITY;
                    CREATE POLICY {PolicyName} ON {table}
                        USING (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid)
                        WITH CHECK (tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            foreach (var table in TenantScopedTables)
            {
                migrationBuilder.Sql($"""
                    DROP POLICY IF EXISTS {PolicyName} ON {table};
                    ALTER TABLE {table} NO FORCE ROW LEVEL SECURITY;
                    ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;
                    """);
            }
        }
    }
}
