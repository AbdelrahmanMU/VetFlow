using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Catalog.Commands.UpdateProduct;
using VetFlow.Application.Catalog.Queries.ManufacturerOptions;
using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Catalog.Queries.ProductNatureOptions;
using VetFlow.Application.Catalog.Queries.UnitOptions;
using VetFlow.Application.Categories.Queries.CategoryOptions;

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
        services.AddSingleton<IValidator<ManufacturerOptionsQuery>, ManufacturerOptionsQueryValidator>();
        services.AddSingleton<IValidator<ProductNatureOptionsQuery>, ProductNatureOptionsQueryValidator>();
        services.AddSingleton<IValidator<UnitOptionsQuery>, UnitOptionsQueryValidator>();
        services.AddSingleton<IValidator<CategoryOptionsQuery>, CategoryOptionsQueryValidator>();
        services.AddSingleton<IValidator<ProductDetailsQuery>, ProductDetailsQueryValidator>();
        services.AddSingleton<IValidator<PossibleDuplicatesQuery>, PossibleDuplicatesQueryValidator>();
        services.AddSingleton<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddSingleton<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        return services;
    }
}
