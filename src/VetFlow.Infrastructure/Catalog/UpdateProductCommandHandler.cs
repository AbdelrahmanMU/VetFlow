using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Commands.UpdateProduct;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Update-product write path (screen S3 edit mode; REQ-CAT-038 first clause /
/// BR-CAT-041, REQ-CAT-003) — the non-audited basic edit (DEC-CAT-031). Loads the
/// aggregate with its unit profile, applies the edit through the domain (every
/// BR-CAT invariant re-enforced there, prices preserved per unit — STD-BE-010), and
/// persists in a single <c>SaveChanges</c> (STD-BE-024). Returns the id, or
/// <c>null</c> when the product does not exist (→ 404). The normalized search
/// columns are refreshed by <see cref="SearchTextInterceptor"/> on the modified row.
/// </summary>
public sealed class UpdateProductCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<UpdateProductCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(entity => entity.Units)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var capabilities = new ProductCapabilities(
            command.IsSplittable,
            command.IsRefrigerated,
            command.HasExpiration,
            command.HasOpenExpiration,
            command.HasOpenExpiration && command.OpenExpirationPeriodDays is { } days
                ? TimeSpan.FromDays(days)
                : null);

        var drafts = command.Units
            .Select(unit => new ProductUnitDraft(
                unit.UnitId,
                unit.Position,
                unit.QuantityInNextUnit,
                unit.IsPurchaseUnit,
                unit.IsSaleUnit,
                unit.Barcode))
            .ToList();

        product.Update(
            command.ArabicName,
            command.EnglishName,
            command.Size,
            command.Concentration,
            command.CategoryId,
            command.ManufacturerId,
            command.NatureId,
            capabilities,
            command.InternalNotes,
            drafts,
            command.StorageUnitId,
            command.DefaultSaleUnitId,
            command.DefaultPurchaseUnitId);

        await dbContext.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
