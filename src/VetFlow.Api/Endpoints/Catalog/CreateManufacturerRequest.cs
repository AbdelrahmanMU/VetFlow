using VetFlow.Application.Catalog.Commands.CreateManufacturer;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// JSON body of POST /api/v1/manufacturers (camelCase, STD-API-032). A pure DTO — the
/// endpoint exposes no domain entity (STD-API-035). A missing name maps to the empty
/// string so the command validator produces the canonical per-field shape.
/// </summary>
public sealed record CreateManufacturerRequest
{
    public string? Name { get; init; }

    public CreateManufacturerCommand ToCommand() => new() { Name = Name ?? string.Empty };
}
