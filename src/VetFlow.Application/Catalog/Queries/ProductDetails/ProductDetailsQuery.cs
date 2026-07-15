using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductDetails;

/// <summary>
/// Read one product by id (screen S2, REQ-CAT-044 — history is always available
/// whatever the status). A <c>null</c> result means the product does not exist
/// and the endpoint answers 404 (problem+json via the status-code handler).
/// </summary>
public sealed record ProductDetailsQuery : IQuery<ProductDetailsDto?>
{
    public required Guid Id { get; init; }
}
