using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Identity;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Scope-owned, gapless numbering (ADR-0022 §6, §12.12). The five database-global sequences are
/// gone, and with them two problems: a second clinic's first invoice would have continued the
/// first clinic's numbering, and a failed save burned a number permanently because
/// <c>nextval</c> does not roll back.
///
/// What must NOT have changed is the format — same prefixes, same six-digit padding — because the
/// numbers a user has already seen are the same numbers.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DocumentNumberingTests(ApiFixture fixture)
{
    private const string NumberedClinicPhone = "01555222333";

    [Fact]
    public async Task A_failed_save_burns_no_number_ADR_0022_6()
    {
        var (categoryId, manufacturerId) = await SeedLookupsAsync();

        var before = await LastValueAsync("PRD");

        // A category that does not exist: the allocation has already happened when the insert
        // fails on the foreign key, which is exactly the case a sequence could not undo.
        var doomed = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            ProductBody(Guid.NewGuid(), manufacturerId, "منتج فاشل"));
        doomed.IsSuccessStatusCode.ShouldBeFalse();

        // The counter is exactly where it was: the failed transaction took its increment with it.
        (await LastValueAsync("PRD")).ShouldBe(before);

        var survivor = await CreateProductAsync(categoryId, manufacturerId, "منتج ناجح");
        SequenceOf(survivor).ShouldBe(before + 1);
    }

    [Fact]
    public async Task Concurrent_creations_take_consecutive_numbers_ADR_0022_6()
    {
        var (categoryId, manufacturerId) = await SeedLookupsAsync();
        var before = await LastValueAsync("PRD");

        var codes = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(index =>
                CreateProductAsync(categoryId, manufacturerId, $"منتج متزامن {index} {Guid.NewGuid():N}")));

        // Consecutive and unique: the row lock serializes the allocations rather than colliding.
        codes.Select(SequenceOf).OrderBy(value => value)
            .ShouldBe([before + 1, before + 2, before + 3, before + 4, before + 5]);
    }

    [Fact]
    public async Task A_second_clinics_first_document_is_number_one_ADR_0022_12_12()
    {
        // The seeded clinic has been numbering documents throughout this suite.
        (await LastValueAsync("PUR")).ShouldBeGreaterThan(0);

        using var newClinic = await SignInAsNewClinicAsync();

        var response = await newClinic.PostAsJsonAsync(
            new Uri("/api/v1/purchase-invoices", UriKind.Relative),
            new
            {
                SupplierName = "مورد العيادة الجديدة",
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("O", CultureInfo.InvariantCulture),
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Not PUR-000002, and not a continuation of anyone else's book. The format is untouched.
        created.RootElement.GetProperty("number").GetString().ShouldBe("PUR-000001");
    }

    private static long SequenceOf(string code) =>
        long.Parse(code[^6..], CultureInfo.InvariantCulture);

    /// <summary>The seeded clinic's counter for a series, read straight from the table.</summary>
    private Task<long> LastValueAsync(string series) =>
        fixture.QueryDbAsync(dbContext => dbContext.Database
            .SqlQueryRaw<long>(
                "SELECT last_value AS \"Value\" FROM document_counters WHERE series = {0}", series)
            .Select(value => value)
            .FirstOrDefaultAsync());

    private async Task<string> CreateProductAsync(Guid categoryId, Guid manufacturerId, string arabicName)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            ProductBody(categoryId, manufacturerId, arabicName));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return created.RootElement.GetProperty("internalCode").GetString()!;
    }

    private async Task<(Guid CategoryId, Guid ManufacturerId)> SeedLookupsAsync()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default, manufacturerId = default;

        await fixture.SeedAsync(dbContext =>
        {
            categoryId = CatalogSeeder.NewCategory(dbContext, $"تصنيف ترقيم {marker}").Id;
            manufacturerId = CatalogSeeder.NewManufacturer(dbContext, $"شركة ترقيم {marker}").Id;
            return Task.CompletedTask;
        });

        return (categoryId, manufacturerId);
    }

    /// <summary>Onboards a clinic that has never numbered anything, and signs in as its owner.</summary>
    private async Task<HttpClient> SignInAsNewClinicAsync()
    {
        var exists = await fixture.QueryDbAsync(dbContext =>
            dbContext.Users.AnyAsync(user => user.PhoneNumber == NumberedClinicPhone));

        if (!exists)
        {
            var tenantId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            await fixture.SeedAsync(dbContext =>
            {
                dbContext.Tenants.Add(new Tenant(tenantId, "Numbering Clinic", "Africa/Cairo"));
                dbContext.Branches.Add(new Branch(branchId, tenantId, "Numbering Main Branch"));
                dbContext.Users.Add(new User(
                    userId, "Numbering Owner", NumberedClinicPhone, new PasswordHasherAdapter().Hash(NumberedClinicPhone)));
                dbContext.Memberships.Add(new Membership(
                    Guid.NewGuid(), tenantId, branchId, userId, MembershipRole.Owner));
                return Task.CompletedTask;
            });
        }

        var client = fixture.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await ApiFixture.SignInAsync(client, NumberedClinicPhone, NumberedClinicPhone));

        return client;
    }

    private static object ProductBody(Guid categoryId, Guid manufacturerId, string arabicName) => new
    {
        ArabicName = arabicName,
        CategoryId = categoryId,
        ManufacturerId = manufacturerId,
        NatureId = SeededCatalogIds.MedicineNature,
        Units = new object[]
        {
            new
            {
                UnitId = SeededCatalogIds.CartonUnit,
                Position = 0,
                QuantityInNextUnit = 10m,
                IsPurchaseUnit = true,
                IsSaleUnit = false,
            },
            new
            {
                UnitId = SeededCatalogIds.BoxUnit,
                Position = 1,
                QuantityInNextUnit = (decimal?)null,
                IsPurchaseUnit = false,
                IsSaleUnit = true,
            },
        },
        StorageUnitId = SeededCatalogIds.BoxUnit,
        DefaultSaleUnitId = SeededCatalogIds.BoxUnit,
        DefaultPurchaseUnitId = SeededCatalogIds.CartonUnit,
    };
}
