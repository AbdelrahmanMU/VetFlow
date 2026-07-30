using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesAndInventoryConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The sales-invoice number sequence (BR-SAL-002): a unique, ascending source for
            // SAL-000001, gaps acceptable — a literal copy of the purchase-invoice sequence
            // (BR-PUR-002) and the Catalog internal-code sequence (DEC-CAT-026).
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS sales_invoice_number_seq AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");

            // NOTE — deliberately hand-edited: the model now maps `xmin` on inventory_batches as
            // the per-batch concurrency token (BR-INV-056, DEC-INV-023). `xmin` is a PostgreSQL
            // *system* column that exists on every table, so the AddColumn/DropColumn statements
            // the scaffolder emitted for it were removed — creating it would fail, and dropping it
            // is impossible. The model snapshot still records the property, so
            // `ef migrations has-pending-model-changes` stays clean. No DDL is needed for it.

            migrationBuilder.CreateTable(
                name: "inventory_consumptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_consumptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sale_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sale_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_line_items_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_consumptions_batch_id",
                table: "inventory_consumptions",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_consumptions_sale_line_id",
                table: "inventory_consumptions",
                column: "sale_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_number",
                table: "sales_invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_sale_date",
                table: "sales_invoices",
                column: "sale_date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_status",
                table: "sales_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sales_line_items_product_id",
                table: "sales_line_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_line_items_sales_invoice_id",
                table: "sales_line_items",
                column: "sales_invoice_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_consumptions");

            migrationBuilder.DropTable(
                name: "sales_line_items");

            migrationBuilder.DropTable(
                name: "sales_invoices");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS sales_invoice_number_seq;");
        }
    }
}
