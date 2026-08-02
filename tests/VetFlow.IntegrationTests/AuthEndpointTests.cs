using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Identity;
using VetFlow.Infrastructure.Organization;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Sign-in end to end (REQ-IDN-002/003, identity/acceptance.md). The properties asserted here are
/// the ones that are cheap to hold and expensive to notice missing: the token's claims, and the
/// fact that <b>three different failures are one indistinguishable rejection</b>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Correct_credentials_return_an_access_token_TS_IDN_001_AC_IDN_001()
    {
        using var client = fixture.CreateAnonymousClient();

        var response = await SignInAsync(client, OrganizationSeeder.PilotOwnerPhoneNumber, OrganizationSeeder.PilotOwnerPhoneNumber);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        payload.RootElement.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        payload.RootElement.GetProperty("displayName").GetString().ShouldBe(OrganizationSeeder.PilotOwnerDisplayName);
    }

    [Fact]
    public async Task The_token_carries_the_scope_and_never_the_password_TS_IDN_002_AC_IDN_002()
    {
        using var client = fixture.CreateAnonymousClient();
        var token = await ApiFixture.SignInAsync(client, OrganizationSeeder.PilotOwnerPhoneNumber, OrganizationSeeder.PilotOwnerPhoneNumber);

        var claims = ReadPayload(token);

        claims.GetProperty("vetflow:user_id").GetString().ShouldNotBeNullOrWhiteSpace();
        claims.GetProperty("vetflow:display_name").GetString().ShouldBe(OrganizationSeeder.PilotOwnerDisplayName);
        claims.GetProperty("vetflow:tenant_id").GetString().ShouldBe(OrganizationSeeder.PilotScope.TenantId.ToString());
        claims.GetProperty("vetflow:branch_id").GetString().ShouldBe(OrganizationSeeder.PilotScope.BranchId.ToString());
        // The role comes from the membership, never from a field on the user (BR-IDN-006, BR-ORG-005).
        claims.GetProperty("vetflow:role").GetString().ShouldBe(nameof(MembershipRole.Owner));

        // Not the password, and not its hash (BR-IDN-002, AC-IDN-004).
        var raw = claims.ToString();
        raw.ShouldNotContain(OrganizationSeeder.PilotOwnerPhoneNumber + "\":\"$");
        raw.ShouldNotContain("password", Case.Insensitive);
        raw.ShouldNotContain("AQAAAA", Case.Insensitive);

        // Twelve hours, exactly as ruled (DEC-IDN-009) — and with no clock skew widening it.
        var lifetime = claims.GetProperty("exp").GetInt64() - claims.GetProperty("nbf").GetInt64();
        lifetime.ShouldBe(12 * 60 * 60);
    }

    [Fact]
    public async Task Unknown_number_wrong_password_and_no_membership_are_one_rejection_TS_IDN_003_004_005_TS_ORG_013()
    {
        // A real user with credentials that work but no membership anywhere (BR-IDN-007).
        const string orphanPhone = "01555999888";
        var alreadySeeded = await fixture.QueryDbAsync(dbContext =>
            dbContext.Users.AnyAsync(user => user.PhoneNumber == orphanPhone));

        if (!alreadySeeded)
        {
            await fixture.SeedAsync(dbContext =>
            {
                dbContext.Users.Add(new User(
                    Guid.NewGuid(), "Orphan User", orphanPhone, new PasswordHasherAdapter().Hash(orphanPhone)));
                return Task.CompletedTask;
            });
        }

        using var client = fixture.CreateAnonymousClient();

        var unknownNumber = await ReadRejectionAsync(client, "01000000000", "whatever");
        var wrongPassword = await ReadRejectionAsync(client, OrganizationSeeder.PilotOwnerPhoneNumber, "not-the-password");
        var noMembership = await ReadRejectionAsync(client, orphanPhone, orphanPhone);

        // Identical, field for field. Any difference tells an attacker which numbers are registered.
        wrongPassword.ShouldBe(unknownNumber);
        noMembership.ShouldBe(unknownNumber);
        unknownNumber.ShouldContain("VTF-IDN-001");
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_refused_TS_IDN_009_AC_IDN_007()
    {
        using var client = fixture.CreateAnonymousClient();
        var token = await ApiFixture.SignInAsync(client, OrganizationSeeder.PilotOwnerPhoneNumber, OrganizationSeeder.PilotOwnerPhoneNumber);

        // The same claims, a different signature: accepted only if the signature were unchecked.
        var forged = token[..token.LastIndexOf('.')] + ".Zm9yZ2VkLXNpZ25hdHVyZQ";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products?page=1&pageSize=5");
        request.Headers.Add("Authorization", $"Bearer {forged}");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // No hint about why (BR-IDN-004): the same shape an absent token gets.
        (await response.Content.ReadAsStringAsync()).ShouldContain("VTF-IDN-002");
    }

    [Fact]
    public async Task The_seeded_owner_exists_hashed_and_is_a_member_TS_IDN_006_TS_IDN_014_AC_IDN_012()
    {
        var owner = await fixture.QueryDbAsync(dbContext => dbContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.PhoneNumber == OrganizationSeeder.PilotOwnerPhoneNumber));

        owner.DisplayName.ShouldBe(OrganizationSeeder.PilotOwnerDisplayName);
        // Stored hashed, never in clear (BR-IDN-002, AC-IDN-004) — even though the Pilot's password
        // equals the phone number by ruling (DEC-IDN-008), which is about *which* password, not how
        // it is kept.
        owner.PasswordHash.ShouldNotBe(OrganizationSeeder.PilotOwnerPhoneNumber);
        owner.PasswordHash.Length.ShouldBeGreaterThan(20);

        var membership = await fixture.QueryDbAsync(dbContext => dbContext.Memberships
            .AsNoTracking()
            .SingleAsync(entry => entry.UserId == owner.Id));

        membership.Role.ShouldBe(MembershipRole.Owner);
        membership.TenantId.ShouldBe(OrganizationSeeder.PilotScope.TenantId);
        membership.BranchId.ShouldBe(OrganizationSeeder.PilotScope.BranchId);
    }

    [Fact]
    public async Task A_phone_number_is_unique_across_the_whole_platform_TS_IDN_015_AC_IDN_013()
    {
        var duplicate = await Should.ThrowAsync<DbUpdateException>(() => fixture.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(new User(
                Guid.NewGuid(),
                "Impostor",
                OrganizationSeeder.PilotOwnerPhoneNumber,
                new PasswordHasherAdapter().Hash("x")));
            return Task.CompletedTask;
        }));

        // Enforced by the database, not by a check the next feature could forget (BR-IDN-001).
        duplicate.InnerException?.Message.ShouldNotBeNull();
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string phoneNumber, string password) =>
        client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new { phoneNumber, password });

    /// <summary>The rejection body with only its correlation-specific fields removed.</summary>
    private static async Task<string> ReadRejectionAsync(HttpClient client, string phoneNumber, string password)
    {
        var response = await SignInAsync(client, phoneNumber, password);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var comparable = problem.RootElement.EnumerateObject()
            .Where(property => property.Name is not "traceId")
            .Select(property => $"{property.Name}={property.Value}")
            .Order(StringComparer.Ordinal);

        return string.Join('\n', comparable);
    }

    private static JsonElement ReadPayload(string token)
    {
        var segment = token.Split('.')[1];
        var padded = segment.PadRight(segment.Length + ((4 - (segment.Length % 4)) % 4), '=')
            .Replace('-', '+')
            .Replace('_', '/');

        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded))).RootElement.Clone();
    }
}
