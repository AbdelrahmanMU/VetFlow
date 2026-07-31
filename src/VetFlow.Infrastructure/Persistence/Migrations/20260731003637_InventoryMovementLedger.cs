using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryMovementLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_consumptions");

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: true),
                    reason_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_batch_id",
                table: "inventory_movements",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_occurred_at_id",
                table: "inventory_movements",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id",
                table: "inventory_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_reference_id",
                table: "inventory_movements",
                column: "reference_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.CreateTable(
                name: "inventory_consumptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_consumptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_consumptions_batch_id",
                table: "inventory_consumptions",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_consumptions_sale_line_id",
                table: "inventory_consumptions",
                column: "sale_line_id");
        }
    }
}
