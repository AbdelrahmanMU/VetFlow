using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using VetFlow.Application.Common;

namespace VetFlow.ArchitectureTests;

/// <summary>
/// Pipeline, repository, and shape conventions (ADR-0014 §5–§8, STD-BE-011,
/// STD-BE-020 … STD-BE-026, STD-BE-042, STD-CS-010).
/// </summary>
public sealed class ConventionTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Catalog.Product).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.CorsOptions).Assembly;

    private static readonly Assembly[] AllAssemblies =
    [
        DomainAssembly,
        ApplicationAssembly,
        InfrastructureAssembly,
        ApiAssembly,
    ];

    [Fact]
    public void Query_handler_implementations_live_in_infrastructure_STD_BE_023()
    {
        var offenders = AllAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(ImplementsQueryHandler)
            .Where(type => !IsPipelineDecorator(type))
            .Where(type => type.Assembly != InfrastructureAssembly)
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Query_handlers_are_named_QueryHandler_STD_BE_020()
    {
        var handlerTypes = AllAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(ImplementsQueryHandler);

        foreach (var handlerType in handlerTypes)
        {
            var backtickIndex = handlerType.Name.IndexOf('`', StringComparison.Ordinal);
            var name = backtickIndex >= 0 ? handlerType.Name[..backtickIndex] : handlerType.Name;
            name.ShouldEndWith("QueryHandler");
        }
    }

    [Fact]
    public void Command_handler_implementations_live_in_infrastructure_STD_BE_023()
    {
        var offenders = AllAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(ImplementsCommandHandler)
            .Where(type => !IsPipelineDecorator(type))
            .Where(type => type.Assembly != InfrastructureAssembly)
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Command_handlers_are_named_CommandHandler_STD_BE_020()
    {
        var handlerTypes = AllAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(ImplementsCommandHandler);

        foreach (var handlerType in handlerTypes)
        {
            var backtickIndex = handlerType.Name.IndexOf('`', StringComparison.Ordinal);
            var name = backtickIndex >= 0 ? handlerType.Name[..backtickIndex] : handlerType.Name;
            name.ShouldEndWith("CommandHandler");
        }
    }

    [Fact]
    public void No_generic_repository_exists_STD_BE_025()
    {
        var offenders = AllAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.Contains("Repository", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Domain_entities_expose_no_public_setters_STD_BE_011()
    {
        var entityTypes = DomainAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsNested: false, IsEnum: false })
            .Where(type => type.Namespace is { } ns && ns.StartsWith("VetFlow.Domain.", StringComparison.Ordinal))
            .Where(type => !IsRecord(type))
            .Where(type => !typeof(Exception).IsAssignableFrom(type));

        foreach (var entityType in entityTypes)
        {
            foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var setter = property.GetSetMethod();
                (setter is null || IsInitOnly(setter)).ShouldBeTrue(
                    $"{entityType.Name}.{property.Name} has a public setter");
            }
        }
    }

    [Fact]
    public void Application_dtos_and_queries_are_records_STD_CS_010()
    {
        var candidates = ApplicationAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsNested: false })
            .Where(type => type.Name.EndsWith("Dto", StringComparison.Ordinal)
                || type.Name.EndsWith("Query", StringComparison.Ordinal));

        foreach (var candidate in candidates)
        {
            IsRecord(candidate).ShouldBeTrue($"{candidate.FullName} must be a record");
        }
    }

    [Fact]
    public void Migrations_live_only_in_infrastructure_STD_BE_042()
    {
        var offenders = AllAssemblies
            .Where(assembly => assembly != InfrastructureAssembly)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.BaseType?.FullName == "Microsoft.EntityFrameworkCore.Migrations.Migration")
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Business_modules_do_not_reach_into_each_other_STD_BE_005()
    {
        // Query handlers are the sanctioned cross-module read path (ADR-0014 §2);
        // shared Persistence and the composition root wire everything.
        AssertNoDependency(
            "VetFlow.Domain.Catalog",
            "VetFlow.Application.Catalog",
            forbidden: ["VetFlow.Domain.Categories", "VetFlow.Application.Categories", "VetFlow.Domain.Purchasing", "VetFlow.Application.Purchasing", "VetFlow.Domain.Inventory", "VetFlow.Application.Inventory"]);

        AssertNoDependency(
            "VetFlow.Domain.Categories",
            "VetFlow.Application.Categories",
            forbidden: ["VetFlow.Domain.Catalog", "VetFlow.Application.Catalog", "VetFlow.Domain.Purchasing", "VetFlow.Application.Purchasing", "VetFlow.Domain.Inventory", "VetFlow.Application.Inventory"]);

        AssertNoDependency(
            "VetFlow.Domain.Purchasing",
            "VetFlow.Application.Purchasing",
            forbidden: ["VetFlow.Domain.Catalog", "VetFlow.Application.Catalog", "VetFlow.Domain.Categories", "VetFlow.Application.Categories", "VetFlow.Domain.Inventory", "VetFlow.Application.Inventory"]);

        // The Inventory write kernel (write-kernel.md, DEC-PUR-008) exposes a public contract and
        // depends on no other module; Purchasing reaches it only through the composition root.
        AssertNoDependency(
            "VetFlow.Domain.Inventory",
            "VetFlow.Application.Inventory",
            forbidden: ["VetFlow.Domain.Catalog", "VetFlow.Application.Catalog", "VetFlow.Domain.Categories", "VetFlow.Application.Categories", "VetFlow.Domain.Purchasing", "VetFlow.Application.Purchasing"]);
    }

    private static void AssertNoDependency(string namespaceA, string namespaceB, string[] forbidden)
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly])
            .That().ResideInNamespaceStartingWith(namespaceA)
            .Or().ResideInNamespaceStartingWith(namespaceB)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        var failing = result.FailingTypeNames ?? [];
        result.IsSuccessful.ShouldBeTrue(string.Join(", ", failing));
    }

    private static bool ImplementsQueryHandler(Type type) =>
        type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

    private static bool ImplementsCommandHandler(Type type) =>
        type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

    private static bool IsPipelineDecorator(Type type) =>
        type.Namespace == "VetFlow.Application.Common.Behaviors";

    private static bool IsRecord(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(method => method.Name == "<Clone>$");

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
}
