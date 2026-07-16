using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManufacturersManagedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default true so any manufacturer created before this migration stays usable
            // and new rows are active by rule (BR-CAT-052); the domain always sets the
            // value explicitly on create, so this only governs the backfill/DB default.
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "manufacturers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers",
                column: "search_text",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "manufacturers");
        }
    }
}
