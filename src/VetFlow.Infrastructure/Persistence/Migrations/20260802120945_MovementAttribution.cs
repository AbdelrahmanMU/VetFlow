using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Every movement records the signed-in user who wrote it (BR-INV-066 as amended 2026-08-02,
    /// REQ-IDN-008, AC-IDN-011). The free-text <c>ActorName</c> beside it is untouched: values
    /// recorded before authentication existed stay readable exactly as they are, and are neither
    /// deleted nor rewritten. It is simply no longer the source of attribution.
    ///
    /// The column is required. The zero default exists only because PostgreSQL needs one to add a
    /// NOT NULL column to a table that already has rows — and the production ledger has none, this
    /// landing before the Pilot's first real entry. Nothing writes that value: an unauthenticated
    /// write is refused rather than attributed to nobody (<c>ActorStampInterceptor</c>).
    /// </summary>
    public partial class MovementAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<Guid>(
                name: "performed_by_user_id",
                table: "inventory_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "performed_by_user_id",
                table: "inventory_movements");
        }
    }
}
