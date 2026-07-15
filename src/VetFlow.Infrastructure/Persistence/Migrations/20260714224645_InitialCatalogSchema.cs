using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    search_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    search_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_natures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    search_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_natures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    arabic_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    english_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    size = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    concentration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nature_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_splittable = table.Column<bool>(type: "boolean", nullable: false),
                    is_refrigerated = table.Column<bool>(type: "boolean", nullable: false),
                    has_expiration = table.Column<bool>(type: "boolean", nullable: false),
                    has_open_expiration = table.Column<bool>(type: "boolean", nullable: false),
                    open_expiration_period = table.Column<TimeSpan>(type: "interval", nullable: true),
                    storage_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_sale_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_purchase_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    search_text = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_product_natures_nature_id",
                        column: x => x.nature_id,
                        principalTable: "product_natures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_units_default_purchase_unit_id",
                        column: x => x.default_purchase_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_units_default_sale_unit_id",
                        column: x => x.default_sale_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_units_storage_unit_id",
                        column: x => x.storage_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    quantity_in_next_unit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    is_purchase_unit = table.Column<bool>(type: "boolean", nullable: false),
                    is_sale_unit = table.Column<bool>(type: "boolean", nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    selling_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_units_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_units_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "product_natures",
                columns: new[] { "id", "name", "search_text" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-4000-8000-000000000001"), "دواء", "دواء" },
                    { new Guid("b1000000-0000-4000-8000-000000000002"), "غذاء", "غذاء" },
                    { new Guid("b1000000-0000-4000-8000-000000000003"), "مستلزم طبي", "مستلزم طبي" },
                    { new Guid("b1000000-0000-4000-8000-000000000004"), "مستلزم حيوانات", "مستلزم حيوانات" },
                    { new Guid("b1000000-0000-4000-8000-000000000005"), "منتج عناية", "منتج عنايه" }
                });

            migrationBuilder.InsertData(
                table: "units",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), "قطعة" },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), "علبة" },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), "كرتونة" },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), "شريط" },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), "قرص" },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), "زجاجة" },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), "سم" },
                    { new Guid("a1000000-0000-4000-8000-000000000008"), "مل" },
                    { new Guid("a1000000-0000-4000-8000-000000000009"), "لتر" },
                    { new Guid("a1000000-0000-4000-8000-000000000010"), "جرام" },
                    { new Guid("a1000000-0000-4000-8000-000000000011"), "كيلوجرام" },
                    { new Guid("a1000000-0000-4000-8000-000000000012"), "شيكارة" },
                    { new Guid("a1000000-0000-4000-8000-000000000013"), "متر" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_search_text",
                table: "categories",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_search_text",
                table: "manufacturers",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_natures_search_text",
                table: "product_natures",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_units_barcode",
                table: "product_units",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "ix_product_units_product_id",
                table: "product_units",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_units_unit_id",
                table: "product_units",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_default_purchase_unit_id",
                table: "products",
                column: "default_purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_default_sale_unit_id",
                table: "products",
                column: "default_sale_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_manufacturer_id",
                table: "products",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_nature_id",
                table: "products",
                column: "nature_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_search_text",
                table: "products",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_status",
                table: "products",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_products_storage_unit_id",
                table: "products",
                column: "storage_unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_units");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "manufacturers");

            migrationBuilder.DropTable(
                name: "product_natures");

            migrationBuilder.DropTable(
                name: "units");
        }
    }
}
