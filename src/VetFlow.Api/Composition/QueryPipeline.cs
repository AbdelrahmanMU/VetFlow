using FluentValidation;
using Microsoft.Extensions.Logging;
using VetFlow.Application.Catalog.Queries.ManufacturerList;
using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Catalog.Queries.ProductNatureOptions;
using VetFlow.Application.Catalog.Queries.UnitOptions;
using VetFlow.Application.Categories.Queries.CategoryList;
using VetFlow.Application.Common;
using VetFlow.Application.Common.Behaviors;
using VetFlow.Application.Inventory.Queries.InventoryProjection;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Application.Purchasing.Queries.PurchaseList;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Purchasing;

namespace VetFlow.Api.Composition;

/// <summary>
/// Composition-root wiring of the query pipeline (ADR-0014 §6, §9): every
/// handler is decorated with validation and logging, explicitly — no
/// assembly scanning, no runtime dispatch.
/// </summary>
public static class QueryPipeline
{
    public static IServiceCollection AddQueryPipeline(this IServiceCollection services)
    {
        services.AddQueryHandler<ProductListQuery, PagedResult<ProductListItemDto>, ProductListQueryHandler>();
        services.AddQueryHandler<ProductDetailsQuery, ProductDetailsDto?, ProductDetailsQueryHandler>();
        services.AddQueryHandler<PossibleDuplicatesQuery, PagedResult<PossibleDuplicateDto>, PossibleDuplicatesQueryHandler>();
        services.AddQueryHandler<ManufacturerListQuery, PagedResult<ManufacturerListItemDto>, ManufacturerListQueryHandler>();
        services.AddQueryHandler<ProductNatureOptionsQuery, PagedResult<LookupOptionDto>, ProductNatureOptionsQueryHandler>();
        services.AddQueryHandler<UnitOptionsQuery, PagedResult<LookupOptionDto>, UnitOptionsQueryHandler>();
        services.AddQueryHandler<CategoryListQuery, PagedResult<CategoryListItemDto>, CategoryListQueryHandler>();
        services.AddQueryHandler<PurchaseListQuery, PagedResult<PurchaseListItemDto>, PurchaseListQueryHandler>();
        services.AddQueryHandler<PurchaseDetailsQuery, PurchaseDetailsDto?, PurchaseDetailsQueryHandler>();
        services.AddQueryHandler<PurchaseLineItemsQuery, IReadOnlyList<PurchaseLineItemDto>?, PurchaseLineItemsQueryHandler>();
        services.AddQueryHandler<InventoryProjectionQuery, PagedResult<InventoryProjectionItemDto>, InventoryProjectionQueryHandler>();
        return services;
    }

    private static void AddQueryHandler<TQuery, TResult, THandler>(this IServiceCollection services)
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.AddScoped<IQueryHandler<TQuery, TResult>>(serviceProvider =>
            new LoggingQueryHandler<TQuery, TResult>(
                new ValidatingQueryHandler<TQuery, TResult>(
                    serviceProvider.GetRequiredService<THandler>(),
                    serviceProvider.GetServices<IValidator<TQuery>>()),
                serviceProvider.GetRequiredService<ILogger<LoggingQueryHandler<TQuery, TResult>>>()));
    }
}
