using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseInvoiceList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The purchase-invoice number sequence (BR-PUR-002): a unique, ascending
            // value per invoice, allocated at persist time. nextval is atomic, so the
            // number is unique under concurrency (the unique index below proves it).
            // Starts at 1 so the first invoice is PUR-000001. Mirrors the Catalog
            // internal-code sequence (DEC-CAT-026).
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS purchase_invoice_number_seq AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");

            migrationBuilder.CreateTable(
                name: "purchase_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    supplier_invoice_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    search_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_invoices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_invoice_date",
                table: "purchase_invoices",
                column: "invoice_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_number",
                table: "purchase_invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_search_text",
                table: "purchase_invoices",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_status",
                table: "purchase_invoices",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_invoices");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS purchase_invoice_number_seq;");
        }
    }
}
