using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;
using VetFlow.Application.Identity;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Identity;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The claim ADR-0022 §2 rests on: one database, and no tenant can reach another's data
/// (REQ-ORG-006, AC-ORG-004, AC-IDN-006). §12.5 calls a cross-tenant read the single
/// highest-severity failure mode in the system, so it is asserted here against a real second
/// clinic that signs in through the real endpoint — not against a mocked context.
///
/// Both nets are covered: the EF query filter (through the API) and PostgreSQL row-level security
/// (against the database directly). They are independent by design, and a test that only exercised
/// one would report a guarantee the system does not have.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantIsolationEndpointTests(ApiFixture fixture)
{
    private const string RivalPhoneNumber = "01555000111";

    [Fact]
    public async Task A_second_tenant_sees_none_of_the_first_tenants_catalog_TS_ORG_004_AC_ORG_004()
    {
        var marker = Guid.NewGuid().ToString("N");
        var productName = $"منتج العزل {marker}";
        await SeedPilotProductAsync(productName);

        // Searched rather than paged: the suite's own products long outgrew one page, and a test
        // that depended on page order would fail for a reason unrelated to isolation.
        (await ProductNamesAsync(fixture.Client, $"&search={marker}")).ShouldContain(productName);

        // A different clinic, signed in for real, sees an empty catalog — without either client
        // naming a tenant anywhere in the request (BR-ORG-003). Both the unfiltered list and the
        // search that just found it come back empty for the second tenant.
        using var rival = await SignInAsRivalTenantAsync();
        (await ProductNamesAsync(rival, $"&search={marker}")).ShouldBeEmpty();
        (await ProductNamesAsync(rival)).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_tenant_id_supplied_by_the_client_changes_nothing_TS_IDN_008_TS_ORG_006()
    {
        var productName = $"منتج الترويسة {Guid.NewGuid():N}";
        await SeedPilotProductAsync(productName);

        using var rival = await SignInAsRivalTenantAsync();

        // Every channel a client could try to speak through: a header, and a query string.
        rival.DefaultRequestHeaders.Add("X-Tenant-Id", PilotTenantId().ToString());

        var names = await ProductNamesAsync(rival, $"&tenantId={PilotTenantId()}");

        names.ShouldBeEmpty();
    }

    [Fact]
    public async Task No_token_reaches_no_business_data_TS_IDN_007_AC_IDN_005()
    {
        using var anonymous = fixture.CreateAnonymousClient();

        var response = await anonymous.GetAsync(new Uri("/api/v1/products?page=1&pageSize=5", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Every_tenant_scoped_table_is_protected_by_row_level_security_ADR_0022_8_2()
    {
        // Driven by the model, so a new scoped entity whose table nobody protected fails here
        // rather than shipping as a hole. §12.7 forbids weakening or skipping this mitigation.
        var scopedTables = await fixture.QueryDbAsync(dbContext => Task.FromResult(
            dbContext.Model.GetEntityTypes()
                .Where(entityType => TenantScope.ReadScope(entityType)
                    is TenantScope.TenantScopeName or TenantScope.BranchScopeName)
                .Select(entityType => entityType.GetTableName()!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList()));

        scopedTables.ShouldNotBeEmpty();

        var unprotected = new List<string>();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var table in scopedTables)
        {
            await using var command = connection.CreateCommand();

            // relforcerowsecurity matters as much as relrowsecurity: the application role owns
            // these tables, and an owner ignores its own policies unless the table forces them.
            command.CommandText = """
                SELECT c.relrowsecurity, c.relforcerowsecurity,
                       (SELECT count(*) FROM pg_policies p
                         WHERE p.tablename = c.relname AND p.policyname = 'tenant_isolation')
                  FROM pg_class c
                 WHERE c.relname = @table
                """;
            command.Parameters.AddWithValue("table", table);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                unprotected.Add($"{table} (no such table)");
                continue;
            }

            if (!reader.GetBoolean(0) || !reader.GetBoolean(1) || reader.GetInt64(2) != 1)
            {
                unprotected.Add(
                    $"{table} (enabled={reader.GetBoolean(0)}, forced={reader.GetBoolean(1)}, policies={reader.GetInt64(2)})");
            }
        }

        unprotected.ShouldBeEmpty(
            "Every tenant-scoped table needs row-level security ENABLEd, FORCEd, and carrying the " +
            "tenant_isolation policy — the second net ADR-0022 §8.2 makes mandatory.");
    }

    [Fact]
    public async Task A_connection_with_no_tenant_published_reads_nothing_ADR_0022_12_7()
    {
        await SeedPilotProductAsync($"منتج الشبكة الثانية {Guid.NewGuid():N}");

        // A raw connection, exactly as a maintenance script or a future report would open one, and
        // deliberately without the session interceptor. The policies must answer "nothing", never
        // "everything": that is what fail-closed means here.
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var blind = connection.CreateCommand();
        blind.CommandText = "SELECT count(*) FROM products";
        Convert.ToInt64(await blind.ExecuteScalarAsync(), provider: null).ShouldBe(0);

        // And with the tenant published, the same connection sees the clinic's rows — proving the
        // zero above is the policy at work, not an empty table.
        await using (var publish = connection.CreateCommand())
        {
            publish.CommandText =
                $"SELECT set_config('{TenantSessionInterceptor.SessionVariable}', @tenant, false)";
            publish.Parameters.AddWithValue("tenant", PilotTenantId().ToString());
            await publish.ExecuteNonQueryAsync();
        }

        await using var scoped = connection.CreateCommand();
        scoped.CommandText = "SELECT count(*) FROM products";
        Convert.ToInt64(await scoped.ExecuteScalarAsync(), provider: null).ShouldBeGreaterThan(0);
    }

    private static Guid PilotTenantId() =>
        VetFlow.Infrastructure.Organization.OrganizationSeeder.PilotScope.TenantId;

    private async Task SeedPilotProductAsync(string arabicName) =>
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {Guid.NewGuid():N}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"شركة {Guid.NewGuid():N}");
            await dbContext.SaveChangesAsync();

            CatalogSeeder.NewProduct(
                dbContext,
                arabicName,
                category.Id,
                manufacturer.Id,
                SeededCatalogIds.MedicineNature);
        });

    /// <summary>
    /// Onboards a second clinic exactly the way ADR-0022 §12.16 says onboarding works — create
    /// tenant, create branch, create owner — and signs in as it through the real endpoint. Nothing
    /// here forges a token: the claims under test are the ones sign-in actually issues.
    /// </summary>
    private async Task<HttpClient> SignInAsRivalTenantAsync()
    {
        var alreadyOnboarded = await fixture.QueryDbAsync(dbContext =>
            dbContext.Users.AnyAsync(user => user.PhoneNumber == RivalPhoneNumber));

        if (!alreadyOnboarded)
        {
            var tenantId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            await fixture.SeedAsync(dbContext =>
            {
                dbContext.Tenants.Add(new Tenant(tenantId, "Rival Clinic", "Africa/Cairo"));
                dbContext.Branches.Add(new Branch(branchId, tenantId, "Rival Main Branch"));
                dbContext.Users.Add(new User(
                    userId,
                    "Rival Owner",
                    RivalPhoneNumber,
                    new PasswordHasherAdapter().Hash(RivalPhoneNumber)));
                dbContext.Memberships.Add(new Membership(
                    Guid.NewGuid(), tenantId, branchId, userId, MembershipRole.Owner));
                return Task.CompletedTask;
            });
        }

        var client = fixture.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await ApiFixture.SignInAsync(client, RivalPhoneNumber, RivalPhoneNumber));

        return client;
    }

    private static async Task<List<string>> ProductNamesAsync(HttpClient client, string extraQuery = "")
    {
        var response = await client.GetAsync(
            new Uri($"/api/v1/products?page=1&pageSize=100{extraQuery}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. json.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("arabicName").GetString() ?? string.Empty)];
    }
}
