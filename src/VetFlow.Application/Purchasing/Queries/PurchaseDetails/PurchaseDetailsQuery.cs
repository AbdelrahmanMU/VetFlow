using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseDetails;

/// <summary>
/// Read one purchase invoice by id (REQ-PUR-002). A <c>null</c> result means the
/// invoice does not exist and the endpoint answers 404 (problem+json via the
/// status-code handler) — a distinct not-found, AC-PUR-005.
/// </summary>
public sealed record PurchaseDetailsQuery : IQuery<PurchaseDetailsDto?>
{
    public required Guid Id { get; init; }
}
