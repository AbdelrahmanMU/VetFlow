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
using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Application.Purchasing.Queries.PurchaseList;

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
        return services;
    }
}
