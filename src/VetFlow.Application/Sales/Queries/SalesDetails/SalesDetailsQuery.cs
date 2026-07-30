using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesDetails;

/// <summary>Read one sales invoice header by id (REQ-SAL-002); <c>null</c> ⇒ 404.</summary>
public sealed record SalesDetailsQuery : IQuery<SalesDetailsDto?>
{
    public required Guid Id { get; init; }
}
