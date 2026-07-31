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
using VetFlow.Application.Inventory.Queries.BatchViewer;
using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Application.Inventory.Queries.InventoryProjection;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Application.Purchasing.Queries.PurchaseList;
using VetFlow.Application.Purchasing.Queries.PurchaseReturnableLines;
using VetFlow.Application.Sales.Queries.SalesDetails;
using VetFlow.Application.Sales.Queries.SalesLineItems;
using VetFlow.Application.Sales.Queries.SalesList;
using VetFlow.Application.Sales.Queries.SalesReturnableLines;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Purchasing;
using VetFlow.Infrastructure.Sales;

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
        services.AddQueryHandler<PurchaseReturnableLinesQuery, IReadOnlyList<PurchaseReturnableLineDto>?, PurchaseReturnableLinesQueryHandler>();
        services.AddQueryHandler<InventoryProjectionQuery, PagedResult<InventoryProjectionItemDto>, InventoryProjectionQueryHandler>();
        services.AddQueryHandler<BatchViewerQuery, BatchViewerResult?, BatchViewerQueryHandler>();
        services.AddQueryHandler<ExpiryMonitoringQuery, PagedResult<ExpiryMonitoringItemDto>, ExpiryMonitoringQueryHandler>();
        services.AddQueryHandler<InventoryHistoryQuery, PagedResult<InventoryHistoryItemDto>, InventoryHistoryQueryHandler>();
        services.AddQueryHandler<SalesListQuery, PagedResult<SalesListItemDto>, SalesListQueryHandler>();
        services.AddQueryHandler<SalesDetailsQuery, SalesDetailsDto?, SalesDetailsQueryHandler>();
        services.AddQueryHandler<SalesLineItemsQuery, IReadOnlyList<SalesLineItemDto>?, SalesLineItemsQueryHandler>();
        services.AddQueryHandler<SalesReturnableLinesQuery, IReadOnlyList<SalesReturnableLineDto>?, SalesReturnableLinesQueryHandler>();
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
