using VetFlow.Application.Categories.Commands.RenameCategory;

namespace VetFlow.Api.Endpoints.Categories;

/// <summary>JSON body of PUT /api/v1/categories/{id} (camelCase, STD-API-032). The id comes from the route.</summary>
public sealed record RenameCategoryRequest
{
    public string? Name { get; init; }

    public RenameCategoryCommand ToCommand(Guid id) => new() { Id = id, Name = Name ?? string.Empty };
}
