using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using VetFlow.Api;
using VetFlow.Api.Composition;
using VetFlow.Api.Endpoints.Catalog;
using VetFlow.Api.Endpoints.Categories;
using VetFlow.Api.Endpoints.Identity;
using VetFlow.Api.Endpoints.Inventory;
using VetFlow.Api.Endpoints.Purchasing;
using VetFlow.Api.Endpoints.Sales;
using VetFlow.Api.Middleware;
using VetFlow.Api.Security;
using VetFlow.Application;
using VetFlow.Application.Common;
using VetFlow.Application.Identity;
using VetFlow.Domain.Identity;
using VetFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// The organizational scope of every request (ADR-0022 §3). Registered as a SINGLETON on purpose:
// EF caches the model, and the global query filters capture this instance once for the process.
// A scoped registration would pin the first request's tenant into the cached model and serve it
// to every later request. One instance implements both abstractions so the tenant, branch and
// user of a request can never disagree with each other.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<RequestTenantContext>();
builder.Services.AddSingleton<ITenantContext>(services => services.GetRequiredService<RequestTenantContext>());
builder.Services.AddSingleton<ICurrentUser>(services => services.GetRequiredService<RequestTenantContext>());
builder.Services.AddQueryPipeline();
builder.Services.AddCommandPipeline();

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options => options.AddPolicy(CorsOptions.PolicyName, policy => policy
    .WithOrigins([.. corsOptions.AllowedOrigins])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var arabicCulture = new CultureInfo("ar");
var englishCulture = new CultureInfo("en");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(arabicCulture);
    options.SupportedCultures = [arabicCulture, englishCulture];
    options.SupportedUICultures = [arabicCulture, englishCulture];
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));

// Token signing (REQ-IDN-003). Like the connection string, the key is required and never
// defaulted: a predictable signing key would let anyone mint a token for any tenant, defeating
// every isolation guarantee in ADR-0022 at once. Refusing to boot is the safe failure.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(
        jwt => !string.IsNullOrWhiteSpace(jwt.SigningKey) && jwt.SigningKey.Length >= 32,
        "Jwt:SigningKey must be configured and at least 32 characters.")
    .ValidateOnStart();

builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt?.Issuer ?? "vetflow",
            ValidAudience = jwt?.Audience ?? "vetflow",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt?.SigningKey ?? new string('0', 32))),

            // No leeway. The default five-minute skew would silently extend every token's life
            // beyond the ruled 12 hours (DEC-IDN-009); server and client share a clock here.
            ClockSkew = TimeSpan.Zero,
        };
    });

// Every endpoint requires an authenticated caller unless it opts out explicitly, and exactly one
// does: sign-in (REQ-IDN-006, BR-IDN-005). Defaulting to "required" means a new endpoint is
// protected by omission rather than exposed by it.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionTranslationMiddleware>();
app.UseStatusCodePages(statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var status = httpContext.Response.StatusCode;
    // A rejected or absent token is answered in the same RFC 9457 shape as everything else, and
    // carries the identity error code so the client can tell "your session ended" from a generic
    // failure and show the ruled message (BR-IDN-008, REQ-IDN-006). Without this arm a 401 read as
    // "internal error", which is both wrong and unactionable.
    var (title, messageKey, errorCode) = status switch
    {
        StatusCodes.Status401Unauthorized =>
            ("Unauthorized", IdentityErrorCodes.NotAuthenticated, IdentityErrorCodes.NotAuthenticated),
        StatusCodes.Status404NotFound => ("Not Found", "error.notFound", null),
        StatusCodes.Status405MethodNotAllowed => ("Method Not Allowed", "error.methodNotAllowed", null),
        _ => ("Request Failed", "error.internal", null),
    };

    return ProblemDetailsWriter.WriteAsync(
        httpContext, status, title, ProblemDetailsWriter.Localize(httpContext, messageKey), errorCode);
});
app.UseSerilogRequestLogging();
app.UseRequestLocalization();
app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapManufacturerEndpoints();
app.MapProductNatureEndpoints();
app.MapUnitEndpoints();
app.MapCategoryEndpoints();
app.MapPurchaseInvoiceEndpoints();
app.MapPurchaseReturnEndpoints();
app.MapInventoryEndpoints();
app.MapSalesInvoiceEndpoints();
app.MapSalesReturnEndpoints();

// Pilot deployment hosting (PRS WS1): when the published Angular bundle is present
// (the Docker image copies it into wwwroot), the API serves it same-origin — no
// second server, no new tool, and CORS becomes moot. Development is untouched:
// there is no wwwroot/index.html there, so nothing below activates, and `ng serve`
// keeps proxying. Unmatched /api/* paths still return the canonical ProblemDetails
// 404 — the SPA fallback deliberately never swallows an API route.
var spaIndex = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
if (File.Exists(spaIndex))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            // No body: UseStatusCodePages turns this into the canonical 404 shape.
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(spaIndex);
    });
}

await VetFlow.Infrastructure.DependencyInjection.ApplyMigrationsIfConfiguredAsync(app.Services);

// The Pilot clinic (ADR-0022 §10). After migrations, before the first request: there is no
// anonymous path, so without a tenant, a branch and an owner nobody could sign in at all.
await VetFlow.Infrastructure.DependencyInjection.SeedOrganizationAsync(app.Services);
await VetFlow.Infrastructure.DependencyInjection.SeedDevelopmentDataIfConfiguredAsync(app.Services);

await app.RunAsync();

/// <summary>Exposes the entry point to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
