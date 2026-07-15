using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Serilog;
using VetFlow.Api;
using VetFlow.Api.Composition;
using VetFlow.Api.Endpoints.Catalog;
using VetFlow.Api.Endpoints.Categories;
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

await VetFlow.Infrastructure.DependencyInjection.ApplyMigrationsIfConfiguredAsync(app.Services);

await app.RunAsync();

/// <summary>Exposes the entry point to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
