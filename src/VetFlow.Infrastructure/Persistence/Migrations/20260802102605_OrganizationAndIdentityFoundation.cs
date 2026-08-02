using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationAndIdentityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_units_products_product_id",
                table: "product_units");

            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_manufacturers_manufacturer_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_line_items_purchase_invoices_purchase_invoice_id",
                table: "purchase_line_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_return_lines_purchase_returns_purchase_return_id",
                table: "purchase_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_line_items_sales_invoices_sales_invoice_id",
                table: "sales_line_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_sales_returns_sales_return_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_number",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_return_lines_sales_return_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "ix_sales_line_items_sales_invoice_id",
                table: "sales_line_items");

            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_number",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_number",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_return_lines_purchase_return_id",
                table: "purchase_return_lines");

            migrationBuilder.DropIndex(
                name: "ix_purchase_line_items_purchase_invoice_id",
                table: "purchase_line_items");

            migrationBuilder.DropIndex(
                name: "ix_purchase_invoices_number",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "ix_products_category_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_internal_code",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_manufacturer_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_units_product_id",
                table: "product_units");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_on_hands",
                table: "product_on_hands");

            migrationBuilder.DropIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers");

            migrationBuilder.DropIndex(
                name: "ix_categories_name_unique",
                table: "categories");

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "sales_returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "sales_line_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_line_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "sales_invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "sales_invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "purchase_returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "purchase_returns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "purchase_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "purchase_return_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "purchase_line_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "purchase_line_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "purchase_invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "purchase_invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_units",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_on_hands",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "product_on_hands",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "manufacturers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "inventory_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "inventory_batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "inventory_batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "ak_sales_returns_tenant_id_id",
                table: "sales_returns",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_sales_invoices_tenant_id_id",
                table: "sales_invoices",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_purchase_returns_tenant_id_id",
                table: "purchase_returns",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_purchase_invoices_tenant_id_id",
                table: "purchase_invoices",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_products_tenant_id_id",
                table: "products",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_on_hands",
                table: "product_on_hands",
                columns: new[] { "tenant_id", "branch_id", "product_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_manufacturers_tenant_id_id",
                table: "manufacturers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_categories_tenant_id_id",
                table: "categories",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_memberships_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_memberships_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_tenant_id_branch_id_number",
                table: "sales_returns",
                columns: new[] { "tenant_id", "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_tenant_id_sales_return_id",
                table: "sales_return_lines",
                columns: new[] { "tenant_id", "sales_return_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_line_items_tenant_id_sales_invoice_id",
                table: "sales_line_items",
                columns: new[] { "tenant_id", "sales_invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_tenant_id_branch_id_number",
                table: "sales_invoices",
                columns: new[] { "tenant_id", "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_tenant_id_branch_id_number",
                table: "purchase_returns",
                columns: new[] { "tenant_id", "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_tenant_id_purchase_return_id",
                table: "purchase_return_lines",
                columns: new[] { "tenant_id", "purchase_return_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_line_items_tenant_id_purchase_invoice_id",
                table: "purchase_line_items",
                columns: new[] { "tenant_id", "purchase_invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_tenant_id_branch_id_number",
                table: "purchase_invoices",
                columns: new[] { "tenant_id", "branch_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_category_id",
                table: "products",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_internal_code",
                table: "products",
                columns: new[] { "tenant_id", "internal_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_id_manufacturer_id",
                table: "products",
                columns: new[] { "tenant_id", "manufacturer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_units_tenant_id_product_id",
                table: "product_units",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers",
                columns: new[] { "tenant_id", "search_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_name_unique",
                table: "categories",
                columns: new[] { "tenant_id", "search_text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_tenant_id",
                table: "branches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_branch_id",
                table: "memberships",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_tenant_id_user_id",
                table: "memberships",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_memberships_user_id",
                table: "memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_id",
                table: "tenants",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_units_products_tenant_id_product_id",
                table: "product_units",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_tenant_id_category_id",
                table: "products",
                columns: new[] { "tenant_id", "category_id" },
                principalTable: "categories",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_manufacturers_tenant_id_manufacturer_id",
                table: "products",
                columns: new[] { "tenant_id", "manufacturer_id" },
                principalTable: "manufacturers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_line_items_purchase_invoices_tenant_id_purchase_in~",
                table: "purchase_line_items",
                columns: new[] { "tenant_id", "purchase_invoice_id" },
                principalTable: "purchase_invoices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_return_lines_purchase_returns_tenant_id_purchase_r~",
                table: "purchase_return_lines",
                columns: new[] { "tenant_id", "purchase_return_id" },
                principalTable: "purchase_returns",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_line_items_sales_invoices_tenant_id_sales_invoice_id",
                table: "sales_line_items",
                columns: new[] { "tenant_id", "sales_invoice_id" },
                principalTable: "sales_invoices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_sales_returns_tenant_id_sales_return_id",
                table: "sales_return_lines",
                columns: new[] { "tenant_id", "sales_return_id" },
                principalTable: "sales_returns",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_units_products_tenant_id_product_id",
                table: "product_units");

            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_tenant_id_category_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_manufacturers_tenant_id_manufacturer_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_line_items_purchase_invoices_tenant_id_purchase_in~",
                table: "purchase_line_items");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_return_lines_purchase_returns_tenant_id_purchase_r~",
                table: "purchase_return_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_line_items_sales_invoices_tenant_id_sales_invoice_id",
                table: "sales_line_items");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_return_lines_sales_returns_tenant_id_sales_return_id",
                table: "sales_return_lines");

            migrationBuilder.DropTable(
                name: "memberships");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_sales_returns_tenant_id_id",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_returns_tenant_id_branch_id_number",
                table: "sales_returns");

            migrationBuilder.DropIndex(
                name: "ix_sales_return_lines_tenant_id_sales_return_id",
                table: "sales_return_lines");

            migrationBuilder.DropIndex(
                name: "ix_sales_line_items_tenant_id_sales_invoice_id",
                table: "sales_line_items");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_sales_invoices_tenant_id_id",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_tenant_id_branch_id_number",
                table: "sales_invoices");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_purchase_returns_tenant_id_id",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_returns_tenant_id_branch_id_number",
                table: "purchase_returns");

            migrationBuilder.DropIndex(
                name: "ix_purchase_return_lines_tenant_id_purchase_return_id",
                table: "purchase_return_lines");

            migrationBuilder.DropIndex(
                name: "ix_purchase_line_items_tenant_id_purchase_invoice_id",
                table: "purchase_line_items");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_purchase_invoices_tenant_id_id",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "ix_purchase_invoices_tenant_id_branch_id_number",
                table: "purchase_invoices");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_products_tenant_id_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_category_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_internal_code",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_id_manufacturer_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_product_units_tenant_id_product_id",
                table: "product_units");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_on_hands",
                table: "product_on_hands");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_manufacturers_tenant_id_id",
                table: "manufacturers");

            migrationBuilder.DropIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_categories_tenant_id_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_name_unique",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_returns");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_return_lines");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "sales_line_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_line_items");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "purchase_returns");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_returns");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "purchase_return_lines");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_return_lines");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "purchase_line_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_line_items");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "purchase_invoices");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "purchase_invoices");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_units");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_on_hands");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "product_on_hands");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "inventory_batches");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "inventory_batches");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "categories");

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_on_hands",
                table: "product_on_hands",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_returns_number",
                table: "sales_returns",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_return_lines_sales_return_id",
                table: "sales_return_lines",
                column: "sales_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_line_items_sales_invoice_id",
                table: "sales_line_items",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_number",
                table: "sales_invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_returns_number",
                table: "purchase_returns",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_return_lines_purchase_return_id",
                table: "purchase_return_lines",
                column: "purchase_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_line_items_purchase_invoice_id",
                table: "purchase_line_items",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_number",
                table: "purchase_invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_internal_code",
                table: "products",
                column: "internal_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_manufacturer_id",
                table: "products",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_units_product_id",
                table: "product_units",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_name_unique",
                table: "manufacturers",
                column: "search_text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_name_unique",
                table: "categories",
                column: "search_text",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_product_units_products_product_id",
                table: "product_units",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_manufacturers_manufacturer_id",
                table: "products",
                column: "manufacturer_id",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_line_items_purchase_invoices_purchase_invoice_id",
                table: "purchase_line_items",
                column: "purchase_invoice_id",
                principalTable: "purchase_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_return_lines_purchase_returns_purchase_return_id",
                table: "purchase_return_lines",
                column: "purchase_return_id",
                principalTable: "purchase_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_line_items_sales_invoices_sales_invoice_id",
                table: "sales_line_items",
                column: "sales_invoice_id",
                principalTable: "sales_invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_return_lines_sales_returns_sales_return_id",
                table: "sales_return_lines",
                column: "sales_return_id",
                principalTable: "sales_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
