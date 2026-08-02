using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Numbering;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Create-product write path (WF-CAT-001, REQ-CAT-001) — the system's first
/// command. It allocates the internal code from the PostgreSQL sequence
/// (DEC-CAT-026 — unique and ascending under concurrency), builds the aggregate
/// through the domain constructor (every BR-CAT invariant is enforced there,
/// STD-BE-010), and persists it in a single <c>SaveChanges</c> (STD-BE-024).
/// The possible-duplicate warning is a separate advisory read and never blocks
/// this write (BR-CAT-042 / DEC-CAT-018).
/// </summary>
public sealed class CreateProductCommandHandler(VetFlowDbContext dbContext, DocumentNumbers documentNumbers)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // One transaction around the allocation and the insert: a failed save returns the code
        // instead of burning it (ADR-0022 §6 — gapless by owner ruling). The product code counts
        // per tenant, not per branch: the catalog is shared across a clinic's branches
        // (DEC-ORG-006), so one product must not acquire a second code at a second branch.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var internalCode = InternalProductCode.Format(
            await documentNumbers.NextAsync(DocumentSeries.ProductCode, cancellationToken));

        var units = command.Units
            .Select(unit => new ProductUnit(
                Guid.NewGuid(),
                unit.UnitId,
                unit.Position,
                unit.QuantityInNextUnit,
                unit.IsPurchaseUnit,
                unit.IsSaleUnit,
                unit.Barcode,
                unit.SellingPrice))
            .ToList();

        var capabilities = new ProductCapabilities(
            command.IsSplittable,
            command.IsRefrigerated,
            command.HasExpiration,
            command.HasOpenExpiration,
            command.HasOpenExpiration && command.OpenExpirationPeriodDays is { } days
                ? TimeSpan.FromDays(days)
                : null);

        var product = new Product(
            Guid.NewGuid(),
            internalCode,
            command.ArabicName,
            command.CategoryId,
            command.ManufacturerId,
            command.NatureId,
            capabilities,
            units,
            command.StorageUnitId,
            command.DefaultSaleUnitId,
            command.DefaultPurchaseUnitId,
            command.EnglishName,
            command.Size,
            command.Concentration,
            command.InternalNotes);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateProductResult { Id = product.Id, InternalCode = product.InternalCode };
    }
}
