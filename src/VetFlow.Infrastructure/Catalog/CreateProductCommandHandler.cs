using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

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
public sealed class CreateProductCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalProductCode.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var internalCode = InternalProductCode.Format(sequenceValue);

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

        return new CreateProductResult { Id = product.Id, InternalCode = product.InternalCode };
    }
}
