using VetFlow.Application.Catalog.Commands.RenameManufacturer;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>JSON body of PUT /api/v1/manufacturers/{id} (camelCase, STD-API-032). The id comes from the route.</summary>
public sealed record RenameManufacturerRequest
{
    public string? Name { get; init; }

    public RenameManufacturerCommand ToCommand(Guid id) => new() { Id = id, Name = Name ?? string.Empty };
}
