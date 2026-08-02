using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Organization;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The structural half of ADR-0022: what the <b>database</b> guarantees, as opposed to what the
/// application remembers to do. Each of these would still hold if every handler were rewritten
/// tomorrow — which is the point of putting them there (organization/acceptance.md).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class OrganizationScopeTests(ApiFixture fixture)
{
    [Fact]
    public async Task Every_scoped_table_carries_its_discriminators_not_null_TS_ORG_001_002()
    {
        var expectations = await fixture.QueryDbAsync(dbContext => Task.FromResult(
            dbContext.Model.GetEntityTypes()
                .Select(entityType => (Table: entityType.GetTableName()!, Scope: TenantScope.ReadScope(entityType)))
                .Where(entry => entry.Scope is TenantScope.TenantScopeName or TenantScope.BranchScopeName)
                .Distinct()
                .ToList()));

        expectations.ShouldNotBeEmpty();

        var offenders = new List<string>();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var (table, scope) in expectations)
        {
            var columns = scope == TenantScope.BranchScopeName
                ? new[] { "tenant_id", "branch_id" }
                : ["tenant_id"];

            foreach (var column in columns)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT is_nullable FROM information_schema.columns
                     WHERE table_name = @table AND column_name = @column
                    """;
                command.Parameters.AddWithValue("table", table);
                command.Parameters.AddWithValue("column", column);

                var nullable = (string?)await command.ExecuteScalarAsync();
                if (nullable != "NO")
                {
                    offenders.Add($"{table}.{column} ({nullable ?? "missing"})");
                }
            }
        }

        offenders.ShouldBeEmpty("A nullable — or absent — discriminator is a row that belongs to no one.");
    }

    [Fact]
    public async Task A_child_row_cannot_belong_to_another_tenant_than_its_parent_TS_ORG_003_AC_ORG_003()
    {
        // A purchase invoice belonging to a different tenant, written with that tenant's scope.
        var strangerTenant = Guid.NewGuid();
        var strangerBranch = Guid.NewGuid();
        var strangerInvoice = Guid.NewGuid();

        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync();
            await SetTenantAsync(connection, strangerTenant);

            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO purchase_invoices
                    (id, tenant_id, branch_id, number, supplier_name, invoice_date, total_amount, status,
                     created_at, search_text)
                VALUES (@id, @tenant, @branch, @number, 'مورد آخر', current_date, 0, 0, now(), 'مورد آخر')
                """;
            insert.Parameters.AddWithValue("id", strangerInvoice);
            insert.Parameters.AddWithValue("tenant", strangerTenant);
            insert.Parameters.AddWithValue("branch", strangerBranch);
            insert.Parameters.AddWithValue("number", $"PUR-{Guid.NewGuid().ToString("N")[..6]}");
            await insert.ExecuteNonQueryAsync();
        }

        // Now attach a line item of the seeded tenant to that invoice. Nothing in code forbids it;
        // the composite foreign key of ADR-0022 §4 makes it impossible (BR-ORG-004, §12.2).
        await using var attacker = new NpgsqlConnection(fixture.ConnectionString);
        await attacker.OpenAsync();
        await SetTenantAsync(attacker, OrganizationSeeder.PilotScope.TenantId);

        await using var crossTenantChild = attacker.CreateCommand();
        crossTenantChild.CommandText = """
            INSERT INTO purchase_line_items
                (id, tenant_id, branch_id, purchase_invoice_id, product_id, product_name,
                 purchase_unit_id, purchase_unit_name, quantity, unit_price, line_total, added_at)
            VALUES (@id, @tenant, @branch, @invoice, @product, 'منتج', @unit, 'علبة', 1, 1, 1, now())
            """;
        crossTenantChild.Parameters.AddWithValue("id", Guid.NewGuid());
        crossTenantChild.Parameters.AddWithValue("tenant", OrganizationSeeder.PilotScope.TenantId);
        crossTenantChild.Parameters.AddWithValue("branch", OrganizationSeeder.PilotScope.BranchId);
        crossTenantChild.Parameters.AddWithValue("invoice", strangerInvoice);
        crossTenantChild.Parameters.AddWithValue("product", Guid.NewGuid());
        crossTenantChild.Parameters.AddWithValue("unit", SeededCatalogIds.BoxUnit);

        var failure = await Should.ThrowAsync<PostgresException>(() => crossTenantChild.ExecuteNonQueryAsync());

        // 23503 = foreign_key_violation: refused by the schema, not by a code path.
        failure.SqlState.ShouldBe("23503");
    }

    [Fact]
    public async Task A_write_is_stamped_with_the_callers_scope_TS_ORG_005_AC_ORG_005()
    {
        var name = $"تصنيف الوسم {Guid.NewGuid():N}";

        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/categories", UriKind.Relative),
            new { Name = name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        // The client passed no tenant anywhere; the row still belongs to the caller's (BR-ORG-003).
        var stamped = await fixture.QueryDbAsync(dbContext => dbContext.Categories
            .Where(category => category.Id == id)
            .Select(category => EF.Property<Guid>(category, TenantScope.TenantIdProperty))
            .SingleAsync());

        stamped.ShouldBe(OrganizationSeeder.PilotScope.TenantId);
    }

    [Fact]
    public async Task Two_clinics_may_use_the_same_category_name_TS_ORG_008_AC_ORG_007()
    {
        var sharedName = $"تصنيف مشترك {Guid.NewGuid():N}";
        var otherTenant = Guid.NewGuid();

        await fixture.SeedAsync(dbContext =>
        {
            dbContext.Tenants.Add(new Tenant(otherTenant, "Same Names Clinic", "Africa/Cairo"));
            return Task.CompletedTask;
        });

        // The seeded clinic takes the name...
        await CreateCategoryAsync(OrganizationSeeder.PilotScope.TenantId, sharedName);

        // ...and the second clinic takes the very same one. Under a global unique index this would
        // fail, and the second customer could not be onboarded (ADR-0022 §12.3).
        await Should.NotThrowAsync(() => CreateCategoryAsync(otherTenant, sharedName));
    }

    [Fact]
    public async Task Document_numbers_are_unique_per_branch_not_globally_TS_ORG_009_AC_ORG_008()
    {
        var indexed = new Dictionary<string, string>(StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var table in new[] { "purchase_invoices", "sales_invoices", "purchase_returns", "sales_returns" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT indexdef FROM pg_indexes
                 WHERE tablename = @table AND indexdef LIKE '%UNIQUE%' AND indexdef LIKE '%number%'
                """;
            command.Parameters.AddWithValue("table", table);
            indexed[table] = (string?)await command.ExecuteScalarAsync() ?? "missing";
        }

        foreach (var (table, definition) in indexed)
        {
            // Leading with the tenant and the branch is what lets two branches each have PUR-000001
            // — and what keeps a number unique inside one branch (REQ-ORG-007).
            definition.ShouldContain("tenant_id", customMessage: table);
            definition.ShouldContain("branch_id", customMessage: table);
        }
    }

    [Fact]
    public async Task The_system_starts_as_one_clinic_with_one_branch_TS_ORG_012_AC_ORG_011()
    {
        var tenant = await fixture.QueryDbAsync(dbContext => dbContext.Tenants
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == OrganizationSeeder.PilotScope.TenantId));

        tenant.Name.ShouldBe(OrganizationSeeder.PilotTenantName);
        tenant.TimeZone.ShouldNotBeNullOrWhiteSpace();

        var branch = await fixture.QueryDbAsync(dbContext => dbContext.Branches
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == OrganizationSeeder.PilotScope.BranchId));

        branch.Name.ShouldBe(OrganizationSeeder.PilotBranchName);
        branch.TenantId.ShouldBe(OrganizationSeeder.PilotScope.TenantId);
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var publish = connection.CreateCommand();
        publish.CommandText = $"SELECT set_config('{TenantSessionInterceptor.SessionVariable}', @tenant, false)";
        publish.Parameters.AddWithValue("tenant", tenantId.ToString());
        await publish.ExecuteNonQueryAsync();
    }

    private async Task CreateCategoryAsync(Guid tenantId, string name)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await SetTenantAsync(connection, tenantId);

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO categories (id, tenant_id, name, is_active, search_text)
            VALUES (@id, @tenant, @name, true, @search)
            """;
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("tenant", tenantId);
        insert.Parameters.AddWithValue("name", name);
        insert.Parameters.AddWithValue("search", name.ToLowerInvariant());
        await insert.ExecuteNonQueryAsync();
    }
}
