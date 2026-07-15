using FluentValidation;
using Microsoft.Extensions.Logging;
using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Catalog.Commands.UpdateProduct;
using VetFlow.Application.Common;
using VetFlow.Application.Common.Behaviors;
using VetFlow.Infrastructure.Catalog;

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
