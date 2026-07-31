using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The PRT- number sequence (BR-PUR-014) — the same mechanism as the PUR- and SAL-
            // sequences, declared here in raw SQL exactly as PurchaseInvoiceList declared its own.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS purchase_return_number_seq AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");

            migrationBuilder.CreateTable(
                name: "purchase_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchase_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_returns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_return_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    purchase_return_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_return_lines_purchase_returns_purchase_return_id",
                        column: x => x.purchase_return_id,
                        principalTable: "purchase_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_batch_id",
                table: "purchase_return_lines",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_purchase_line_item_id",
                table: "purchase_return_lines",
                column: "purchase_line_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_purchase_return_id",
                table: "purchase_return_lines",
                column: "purchase_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_number",
                table: "purchase_returns",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_purchase_invoice_id",
                table: "purchase_returns",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_status",
                table: "purchase_returns",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_return_lines");

            migrationBuilder.DropTable(
                name: "purchase_returns");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS purchase_return_number_seq;");
        }
    }
}
