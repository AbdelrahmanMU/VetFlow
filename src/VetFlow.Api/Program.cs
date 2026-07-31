using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Serilog;
using VetFlow.Api;
using VetFlow.Api.Composition;
using VetFlow.Api.Endpoints.Catalog;
using VetFlow.Api.Endpoints.Categories;
using VetFlow.Api.Endpoints.Inventory;
using VetFlow.Api.Endpoints.Purchasing;
using VetFlow.Api.Endpoints.Sales;
using VetFlow.Api.Middleware;
using VetFlow.Application;
using VetFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
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

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionTranslationMiddleware>();
app.UseStatusCodePages(statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var status = httpContext.Response.StatusCode;
    var (title, messageKey) = status switch
    {
        StatusCodes.Status404NotFound => ("Not Found", "error.notFound"),
        StatusCodes.Status405MethodNotAllowed => ("Method Not Allowed", "error.methodNotAllowed"),
        _ => ("Request Failed", "error.internal"),
    };

    return ProblemDetailsWriter.WriteAsync(
        httpContext, status, title, ProblemDetailsWriter.Localize(httpContext, messageKey), errorCode: null);
});
app.UseSerilogRequestLogging();
app.UseRequestLocalization();
app.UseCors(CorsOptions.PolicyName);

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
await VetFlow.Infrastructure.DependencyInjection.SeedDevelopmentDataIfConfiguredAsync(app.Services);

await app.RunAsync();

/// <summary>Exposes the entry point to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
