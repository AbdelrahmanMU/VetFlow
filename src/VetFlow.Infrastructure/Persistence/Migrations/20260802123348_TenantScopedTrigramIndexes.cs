using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantScopedTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_search_text",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_purchase_invoices_search_text",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "ix_products_arabic_name_normalized",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_search_text",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_manufacturers_search_text",
                table: "manufacturers");

            migrationBuilder.DropIndex(
                name: "ix_categories_search_text",
                table: "categories");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_tenant_id_search_text",
                table: "sales_invoices",
                columns: new[] { "tenant_id", "search_text" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_tenant_id_search_text",
                table: "purchase_invoices",
                columns: new[] { "tenant_id", "search_text" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_arabic_name_normalized",
                table: "products",
                columns: new[] { "tenant_id", "arabic_name_normalized" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_search_text",
                table: "products",
                columns: new[] { "tenant_id", "search_text" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_tenant_id_search_text",
                table: "manufacturers",
                columns: new[] { "tenant_id", "search_text" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_tenant_id_search_text",
                table: "categories",
                columns: new[] { "tenant_id", "search_text" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "uuid_ops", "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_tenant_id_search_text",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_purchase_invoices_tenant_id_search_text",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_arabic_name_normalized",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_search_text",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_manufacturers_tenant_id_search_text",
                table: "manufacturers");

            migrationBuilder.DropIndex(
                name: "ix_categories_tenant_id_search_text",
                table: "categories");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_search_text",
                table: "sales_invoices",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_search_text",
                table: "purchase_invoices",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_arabic_name_normalized",
                table: "products",
                column: "arabic_name_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_search_text",
                table: "products",
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
                name: "ix_categories_search_text",
                table: "categories",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }
    }
}
