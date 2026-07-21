using FluentValidation;
using Microsoft.Extensions.Logging;
using VetFlow.Application.Catalog.Commands.CreateManufacturer;
using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Catalog.Commands.RenameManufacturer;
using VetFlow.Application.Catalog.Commands.SetManufacturerActive;
using VetFlow.Application.Catalog.Commands.UpdateProduct;
using VetFlow.Application.Categories.Commands.CreateCategory;
using VetFlow.Application.Categories.Commands.RenameCategory;
using VetFlow.Application.Categories.Commands.SetCategoryActive;
using VetFlow.Application.Common;
using VetFlow.Application.Common.Behaviors;
using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Application.Purchasing.Commands.RemovePurchaseLineItem;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Purchasing;

namespace VetFlow.Api.Composition;

/// <summary>
/// Composition-root wiring of the command pipeline (ADR-0014 §6, §9) — the
/// write-side mirror of <see cref="QueryPipeline"/>: every handler is decorated
/// with validation and logging, explicitly — no assembly scanning, no runtime
/// dispatch.
/// </summary>
public static class CommandPipeline
{
    public static IServiceCollection AddCommandPipeline(this IServiceCollection services)
    {
        services.AddCommandHandler<CreateProductCommand, CreateProductResult, CreateProductCommandHandler>();
        services.AddCommandHandler<UpdateProductCommand, Guid?, UpdateProductCommandHandler>();
        services.AddCommandHandler<CreateCategoryCommand, Guid, CreateCategoryCommandHandler>();
        services.AddCommandHandler<RenameCategoryCommand, Guid?, RenameCategoryCommandHandler>();
        services.AddCommandHandler<SetCategoryActiveCommand, Guid?, SetCategoryActiveCommandHandler>();
        services.AddCommandHandler<CreateManufacturerCommand, Guid, CreateManufacturerCommandHandler>();
        services.AddCommandHandler<RenameManufacturerCommand, Guid?, RenameManufacturerCommandHandler>();
        services.AddCommandHandler<SetManufacturerActiveCommand, Guid?, SetManufacturerActiveCommandHandler>();
        services.AddCommandHandler<CreatePurchaseInvoiceCommand, CreatePurchaseInvoiceResult, CreatePurchaseInvoiceCommandHandler>();
        services.AddCommandHandler<AddPurchaseLineItemCommand, Guid?, AddPurchaseLineItemCommandHandler>();
        services.AddCommandHandler<RemovePurchaseLineItemCommand, Guid?, RemovePurchaseLineItemCommandHandler>();
        return services;
    }

    private static void AddCommandHandler<TCommand, TResult, THandler>(this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.AddScoped<ICommandHandler<TCommand, TResult>>(serviceProvider =>
            new LoggingCommandHandler<TCommand, TResult>(
                new ValidatingCommandHandler<TCommand, TResult>(
                    serviceProvider.GetRequiredService<THandler>(),
                    serviceProvider.GetServices<IValidator<TCommand>>()),
                serviceProvider.GetRequiredService<ILogger<LoggingCommandHandler<TCommand, TResult>>>()));
    }
}
