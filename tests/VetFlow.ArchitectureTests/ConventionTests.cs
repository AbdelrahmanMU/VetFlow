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
    public void Movement_contract_enums_mirror_their_domain_enums_BR_INV_065()
    {
        // The history DTO mirrors the domain enums so the wire contract is not pinned to a domain
        // type (the SalesInvoiceStatusDto precedent), and the handler casts between them. A cast is
        // only safe while the two agree exactly — so the agreement is asserted, not assumed. Adding
        // a movement type or source on one side alone fails here rather than silently mislabelling
        // a row on the history screen.
        AssertMirrors<Domain.Inventory.InventoryMovementType, Application.Inventory.Queries.InventoryHistory.InventoryMovementTypeDto>();
        AssertMirrors<Domain.Inventory.InventoryMovementSource, Application.Inventory.Queries.InventoryHistory.InventoryMovementSourceDto>();
    }

    [Fact]
    public void A_return_carries_no_reason_and_no_money_BR_INV_067_DEC_INV_035()
    {
        // Two rules that are easiest to break by adding a "harmless" field later, so they are
        // asserted at the shape of the contract rather than trusted to review:
        //   • returns carry no reason code at all (BR-INV-067) — «مستندها هو سياقها»;
        //   • a return has no financial effect whatsoever (DEC-INV-035), so no amount belongs on
        //     the document, its lines, or the wire contract the screen binds to.
        // A reason or price field appearing on any of them fails here. Both returns are listed:
        // the sales side is the more tempting of the two, because its original line does carry a
        // unit price and a line total, and neither belongs on the return or its screen (ui.md).
        string[] forbidden = ["reason", "price", "amount", "total", "cost", "money"];

        Type[] returnTypes =
        [
            typeof(Domain.Purchasing.PurchaseReturn),
            typeof(Domain.Purchasing.PurchaseReturnLine),
            typeof(Application.Purchasing.Commands.CreatePurchaseReturn.CreatePurchaseReturnCommand),
            typeof(Application.Purchasing.Commands.AddPurchaseReturnLine.AddPurchaseReturnLineCommand),
            typeof(Application.Purchasing.Queries.PurchaseReturnableLines.PurchaseReturnableLineDto),
            typeof(Domain.Sales.SalesReturn),
            typeof(Domain.Sales.SalesReturnLine),
            typeof(Application.Sales.Commands.CreateSalesReturn.CreateSalesReturnCommand),
            typeof(Application.Sales.Commands.AddSalesReturnLine.AddSalesReturnLineCommand),
            typeof(Application.Sales.Queries.SalesReturnableLines.SalesReturnableLineDto),
        ];

        var offenders = returnTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => forbidden.Any(word =>
                    property.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void A_purchase_return_line_never_accepts_a_batch_from_the_caller_BR_PUR_017()
    {
        // The destination batch is a fact of the original purchase line (DEC-PUR-008), never a
        // choice — BR-PUR-017 and BR-INV-069 forbid selecting one, and FEFO plays no part in a
        // return. The domain line records a BatchId; the *command* must not accept one, or a caller
        // could push stock into the wrong batch. This asserts the asymmetry deliberately.
        typeof(Domain.Purchasing.PurchaseReturnLine)
            .GetProperty("BatchId").ShouldNotBeNull();

        typeof(Application.Purchasing.Commands.AddPurchaseReturnLine.AddPurchaseReturnLineCommand)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.Name.Contains("Batch", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse();
    }

    [Fact]
    public void A_sales_return_holds_no_batch_reference_anywhere_BR_SAL_013_BR_SAL_017()
    {
        // The asymmetry with the purchase return above is deliberate and is asserted rather than
        // left to review. Purchasing records a BatchId on its line because one purchase line creates
        // exactly one batch (DEC-PUR-008). Sales records **none**: FEFO may have split the sale line
        // across several batches (BR-SAL-017), and Sales may not store a batch reference at all
        // (BR-SAL-013, DEC-SAL-006). The destinations are derived by Inventory at commit and live in
        // the movement ledger — so a BatchId appearing on any of these three is a boundary breach,
        // not a convenience.
        Type[] salesReturnTypes =
        [
            typeof(Domain.Sales.SalesReturn),
            typeof(Domain.Sales.SalesReturnLine),
            typeof(Application.Sales.Commands.AddSalesReturnLine.AddSalesReturnLineCommand),
        ];

        var offenders = salesReturnTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name.Contains("Batch", StringComparison.OrdinalIgnoreCase))
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void The_adjustment_contract_reasons_are_exactly_the_adjustment_subset_BR_INV_067()
    {
        // The API enum is deliberately narrower than the domain vocabulary: a caller cannot even
        // express a write-off-only reason on an adjustment (DEC-INV-031). This asserts the two stay
        // in step — adding a reason to one side alone fails here rather than opening a hole that
        // only the runtime validator would catch.
        var contract = Enum.GetValues<Application.Inventory.Commands.AdjustInventory.AdjustmentReason>()
            .Select(reason => (Domain.Inventory.InventoryMovementReason)(int)reason)
            .OrderBy(reason => reason)
            .ToList();

        contract.ShouldBe([.. Domain.Inventory.InventoryMovementReasons.ForAdjustment.OrderBy(reason => reason)]);
    }

    [Fact]
    public void The_write_off_contract_reasons_are_exactly_the_write_off_subset_BR_INV_067()
    {
        // The mirror of the adjustment guard. «موجود» on a write-off would be a contradiction, and
        // «منتهي الصلاحية» on an adjustment would merge two lists the owner ruled separately.
        var contract = Enum.GetValues<Application.Inventory.Commands.WriteOffInventory.WriteOffReason>()
            .Select(reason => (Domain.Inventory.InventoryMovementReason)(int)reason)
            .OrderBy(reason => reason)
            .ToList();

        contract.ShouldBe([.. Domain.Inventory.InventoryMovementReasons.ForWriteOff.OrderBy(reason => reason)]);
    }

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
        AssertModuleIsIsolated("Catalog");
        AssertModuleIsIsolated("Categories");
        AssertModuleIsIsolated("Purchasing");

        // The Inventory write kernel and its consumption contract (write-kernel.md, DEC-PUR-008,
        // DEC-INV-019) expose public contracts and depend on no other module; Purchasing and Sales
        // reach them only through the composition root.
        AssertModuleIsIsolated("Inventory");

        // Sales, in both directions (Sprint 7): Sales must not reach into Inventory — putting FEFO
        // or batch knowledge in Sales would break the boundary the owner ruled (DEC-SAL-006,
        // BR-SAL-013) — and no module may reach into Sales. The second direction matters because
        // Inventory now stores a sale-line id (REQ-INV-008): it must stay a plain Guid, never a
        // Sales type.
        AssertModuleIsIsolated("Sales");

        // The Dashboard is the module most likely to break this rule, so it is held to it
        // hardest. It composes reads that Inventory, Sales and Purchasing own (REQ-INV-013,
        // REQ-SAL-006, REQ-PUR-007) — but that composition lives in Infrastructure, the
        // sanctioned cross-module read path (ADR-0014 §2). VetFlow.Application.Dashboard is a
        // parameterless query and a primitive DTO, and this assertion is what keeps it that
        // way: the moment someone imports an Inventory type to "just reuse the enum", the
        // build fails. That is BR-DSH-001 / DEC-DSH-001 made structural rather than advisory.
        AssertModuleIsIsolated("Dashboard");
    }

    /// <summary>
    /// A module's Domain and Application namespaces must not depend on any <b>other</b> module's
    /// Domain or Application namespace (STD-BE-005, ADR-0014 §2). Because this runs for every
    /// module, each pair is asserted in both directions.
    /// </summary>
    private static void AssertModuleIsIsolated(string module)
    {
        string[] modules = ["Catalog", "Categories", "Purchasing", "Inventory", "Sales", "Dashboard"];

        var forbidden = modules
            .Where(other => !string.Equals(other, module, StringComparison.Ordinal))
            .SelectMany(other => new[] { $"VetFlow.Domain.{other}", $"VetFlow.Application.{other}" })
            .ToArray();

        AssertNoDependency($"VetFlow.Domain.{module}", $"VetFlow.Application.{module}", forbidden);
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

    /// <summary>Asserts two enums carry the identical set of (name, value) pairs.</summary>
    private static void AssertMirrors<TDomain, TContract>()
        where TDomain : struct, Enum
        where TContract : struct, Enum
    {
        static string[] Pairs<TEnum>() where TEnum : struct, Enum =>
            [.. Enum.GetValues<TEnum>()
                .Select(value => $"{value}={Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}")
                .OrderBy(pair => pair, StringComparer.Ordinal)];

        Pairs<TContract>().ShouldBe(Pairs<TDomain>());
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
