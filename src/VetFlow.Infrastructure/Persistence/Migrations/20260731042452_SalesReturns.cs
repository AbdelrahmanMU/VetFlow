using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The SRT- number sequence (BR-SAL-014) — the same mechanism as the SAL- and PRT-
            // sequences, declared here in raw SQL exactly as they were.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS sales_return_number_seq AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");

            migrationBuilder.CreateTable(
                name: "sales_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    return_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_returns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_return_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sales_return_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_return_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_return_lines_sales_returns_sales_return_id",
                        column: x => x.sales_return_id,
                        principalTable: "sales_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_sales_line_item_id",
                table: "sales_return_lines",
                column: "sales_line_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_sales_return_id",
                table: "sales_return_lines",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_number",
                table: "sales_returns",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_sales_invoice_id",
                table: "sales_returns",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_status",
                table: "sales_returns",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_return_lines");

            migrationBuilder.DropTable(
                name: "sales_returns");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS sales_return_number_seq;");
        }
    }
}
