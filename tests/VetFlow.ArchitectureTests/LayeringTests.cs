using NetArchTest.Rules;
using Shouldly;

namespace VetFlow.ArchitectureTests;

/// <summary>
/// The dependency rule (ADR-0014 §1, STD-BE-001 … STD-BE-004): dependencies
/// point inward, and a failing test here is fixed by changing the code —
/// never by changing the test (ADR-0016).
/// </summary>
public sealed class LayeringTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Domain.Catalog.Product).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(Api.CorsOptions).Assembly;

    private static readonly System.Reflection.Assembly[] ProductionAssemblies =
    [
        DomainAssembly,
        ApplicationAssembly,
        InfrastructureAssembly,
        ApiAssembly,
    ];

    [Fact]
    public void Domain_references_no_framework_and_no_outer_layer_STD_BE_001()
    {
        AssertClean(Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "VetFlow.Application",
                "VetFlow.Infrastructure",
                "VetFlow.Api",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions",
                "FluentValidation",
                "Serilog")
            .GetResult());
    }

    [Fact]
    public void Application_never_references_infrastructure_or_the_database_STD_BE_002_STD_BE_003()
    {
        AssertClean(Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "VetFlow.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult());
    }

    [Fact]
    public void Api_never_touches_domain_entities_directly_STD_BE_004()
    {
        AssertClean(Types.InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "VetFlow.Domain.Catalog.Product",
                "VetFlow.Domain.Catalog.Manufacturer",
                "VetFlow.Domain.Catalog.Unit",
                "VetFlow.Domain.Categories.Category")
            .GetResult());
    }

    [Fact]
    public void Api_does_not_use_ef_core_directly_STD_BE_003()
    {
        AssertClean(Types.InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult());
    }

    [Fact]
    public void Production_code_depends_on_no_forbidden_library_STD_BE_020_STD_BE_028()
    {
        // The forbidden-library ban is enforced here, not merely documented:
        // ADR-0014 — "a rule without a test is a wish". MediatR (implicit dispatch),
        // AutoMapper (implicit mapping), and FluentAssertions (2025 licensing) are
        // Forbidden by backend-standards.md. NgRx is a frontend package invisible to
        // NetArchTest; it is banned in the frontend and enforced by ESLint.
        string[] forbidden = ["MediatR", "AutoMapper", "FluentAssertions"];

        foreach (var assembly in ProductionAssemblies)
        {
            AssertClean(Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult());
        }
    }

    private static void AssertClean(TestResult result)
    {
        var failing = result.FailingTypeNames ?? [];
        result.IsSuccessful.ShouldBeTrue(string.Join(", ", failing));
    }
}
