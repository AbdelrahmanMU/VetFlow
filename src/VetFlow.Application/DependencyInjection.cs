using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VetFlow.Application.Catalog.Commands.CreateManufacturer;
using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Catalog.Commands.RenameManufacturer;
using VetFlow.Application.Catalog.Commands.UpdateProduct;
using VetFlow.Application.Catalog.Queries.ManufacturerList;
using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Catalog.Queries.ProductNatureOptions;
using VetFlow.Application.Catalog.Queries.UnitOptions;
using VetFlow.Application.Categories.Commands.CreateCategory;
using VetFlow.Application.Categories.Commands.RenameCategory;
using VetFlow.Application.Categories.Queries.CategoryList;
using VetFlow.Application.Inventory.Commands.AdjustInventory;
using VetFlow.Application.Inventory.Commands.WriteOffInventory;
using VetFlow.Application.Inventory.Queries.BatchViewer;
using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Application.Inventory.Queries.InventoryProjection;
using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;
using VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;
using VetFlow.Application.Purchasing.Commands.ReceivePurchaseInvoice;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Application.Purchasing.Queries.PurchaseList;
using VetFlow.Application.Sales.Commands.AddSalesLineItem;
using VetFlow.Application.Sales.Commands.AddSalesReturnLine;
using VetFlow.Application.Sales.Commands.CommitSalesInvoice;
using VetFlow.Application.Sales.Commands.CreateSalesInvoice;
using VetFlow.Application.Sales.Commands.CreateSalesReturn;
using VetFlow.Application.Sales.Queries.SalesDetails;
using VetFlow.Application.Sales.Queries.SalesLineItems;
using VetFlow.Application.Sales.Queries.SalesList;

namespace VetFlow.Application;

/// <summary>
/// The Application layer's single registration extension (ADR-0014 §9),
/// invoked only by the composition root. Registrations are explicit — no
/// assembly scanning (principle 4).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<ProductListQuery>, ProductListQueryValidator>();
        services.AddSingleton<IValidator<ManufacturerListQuery>, ManufacturerListQueryValidator>();
        services.AddSingleton<IValidator<ProductNatureOptionsQuery>, ProductNatureOptionsQueryValidator>();
        services.AddSingleton<IValidator<UnitOptionsQuery>, UnitOptionsQueryValidator>();
        services.AddSingleton<IValidator<CategoryListQuery>, CategoryListQueryValidator>();
        services.AddSingleton<IValidator<ProductDetailsQuery>, ProductDetailsQueryValidator>();
        services.AddSingleton<IValidator<PossibleDuplicatesQuery>, PossibleDuplicatesQueryValidator>();
        services.AddSingleton<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddSingleton<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddSingleton<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddSingleton<IValidator<RenameCategoryCommand>, RenameCategoryCommandValidator>();
        services.AddSingleton<IValidator<CreateManufacturerCommand>, CreateManufacturerCommandValidator>();
        services.AddSingleton<IValidator<RenameManufacturerCommand>, RenameManufacturerCommandValidator>();
        services.AddSingleton<IValidator<PurchaseListQuery>, PurchaseListQueryValidator>();
        services.AddSingleton<IValidator<PurchaseDetailsQuery>, PurchaseDetailsQueryValidator>();
        services.AddSingleton<IValidator<PurchaseLineItemsQuery>, PurchaseLineItemsQueryValidator>();
        services.AddSingleton<IValidator<CreatePurchaseInvoiceCommand>, CreatePurchaseInvoiceCommandValidator>();
        services.AddSingleton<IValidator<AddPurchaseLineItemCommand>, AddPurchaseLineItemCommandValidator>();
        services.AddSingleton<IValidator<ReceivePurchaseInvoiceCommand>, ReceivePurchaseInvoiceCommandValidator>();
        services.AddSingleton<IValidator<InventoryProjectionQuery>, InventoryProjectionQueryValidator>();
        services.AddSingleton<IValidator<BatchViewerQuery>, BatchViewerQueryValidator>();
        services.AddSingleton<IValidator<ExpiryMonitoringQuery>, ExpiryMonitoringQueryValidator>();
        services.AddSingleton<IValidator<InventoryHistoryQuery>, InventoryHistoryQueryValidator>();
        services.AddSingleton<IValidator<AdjustInventoryCommand>, AdjustInventoryCommandValidator>();
        services.AddSingleton<IValidator<WriteOffInventoryCommand>, WriteOffInventoryCommandValidator>();
        services.AddSingleton<IValidator<SalesListQuery>, SalesListQueryValidator>();
        services.AddSingleton<IValidator<SalesDetailsQuery>, SalesDetailsQueryValidator>();
        services.AddSingleton<IValidator<SalesLineItemsQuery>, SalesLineItemsQueryValidator>();
        services.AddSingleton<IValidator<CreateSalesInvoiceCommand>, CreateSalesInvoiceCommandValidator>();
        services.AddSingleton<IValidator<AddSalesLineItemCommand>, AddSalesLineItemCommandValidator>();
        services.AddSingleton<IValidator<CommitSalesInvoiceCommand>, CommitSalesInvoiceCommandValidator>();

        // The four return validators (Epic 2 · C5/C6 — AC-PUR-019/021, AC-SAL-014/016). Validators
        // are resolved with GetServices, so an unregistered one is silently *no validation at all*:
        // the C5 pair was written and never registered, which left a missing originating invoice to
        // fall through to the handler's `!` and surface as a 500 instead of the documented per-field
        // 400. Registering is what makes a validator exist.
        services.AddSingleton<IValidator<CreatePurchaseReturnCommand>, CreatePurchaseReturnCommandValidator>();
        services.AddSingleton<IValidator<AddPurchaseReturnLineCommand>, AddPurchaseReturnLineCommandValidator>();
        services.AddSingleton<IValidator<CreateSalesReturnCommand>, CreateSalesReturnCommandValidator>();
        services.AddSingleton<IValidator<AddSalesReturnLineCommand>, AddSalesReturnLineCommandValidator>();
        return services;
    }
}
