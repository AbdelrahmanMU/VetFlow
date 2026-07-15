using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductWritePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The internal-code sequence (DEC-CAT-026): a unique, ascending value
            // per product, allocated at persist time. nextval is atomic, so the
            // code is unique under concurrency (the unique index below proves it).
            // Starts at 1 so the first product is PRD-000001.
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS product_internal_code_seq AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 1 NO MAXVALUE NO CYCLE;");

            migrationBuilder.AddColumn<string>(
                name: "arabic_name_normalized",
                table: "products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "internal_code",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "internal_notes",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_arabic_name_normalized",
                table: "products",
                column: "arabic_name_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_internal_code",
                table: "products",
                column: "internal_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_arabic_name_normalized",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_internal_code",
                table: "products");

            migrationBuilder.DropColumn(
                name: "arabic_name_normalized",
                table: "products");

            migrationBuilder.DropColumn(
                name: "internal_code",
                table: "products");

            migrationBuilder.DropColumn(
                name: "internal_notes",
                table: "products");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS product_internal_code_seq;");
        }
    }
}
