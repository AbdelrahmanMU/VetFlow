using VetFlow.Application.Categories.Commands.CreateCategory;

namespace VetFlow.Api.Endpoints.Categories;

/// <summary>
/// JSON body of POST /api/v1/categories (camelCase, STD-API-032). A pure DTO — the
/// endpoint exposes no domain entity (STD-API-035). A missing name maps to the empty
/// string so the command validator produces the canonical per-field shape.
/// </summary>
public sealed record CreateCategoryRequest
{
    public string? Name { get; init; }

    public CreateCategoryCommand ToCommand() => new() { Name = Name ?? string.Empty };
}
