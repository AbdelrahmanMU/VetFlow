namespace VetFlow.Application.Catalog.Commands.CreateProduct;

/// <summary>
/// The lightweight result of creating a product (STD-BE-021 — never a read DTO):
/// the new id and the system-generated internal code (DEC-CAT-026).
/// </summary>
public sealed record CreateProductResult
{
    public required Guid Id { get; init; }

    public required string InternalCode { get; init; }
}
