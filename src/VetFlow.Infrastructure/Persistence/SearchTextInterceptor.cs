using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Infrastructure.Persistence;

/// <summary>
/// Maintains the normalized search columns at write time (STD-BE-044) for every
/// searchable entity, so no caller can forget them.
/// </summary>
public sealed class SearchTextInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySearchText(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySearchText(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySearchText(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var searchText = entry.Entity switch
            {
                Product product => ArabicSearchText.Normalize(product.ArabicName, product.EnglishName),
                Manufacturer manufacturer => ArabicSearchText.Normalize(manufacturer.Name),
                ProductNature nature => ArabicSearchText.Normalize(nature.Name),
                Category category => ArabicSearchText.Normalize(category.Name),
                // Supplier name + the supplier's invoice reference are searchable
                // (BR-PUR-004); notes are deliberately excluded.
                PurchaseInvoice invoice => ArabicSearchText.Normalize(invoice.SupplierName, invoice.SupplierInvoiceReference),
                _ => null,
            };

            if (searchText is not null)
            {
                entry.Property(SearchableText.PropertyName).CurrentValue = searchText;
            }

            // The Arabic-name-only column backs the possible-duplicate advisory
            // (DEC-CAT-027), which matches on the Arabic name specifically.
            if (entry.Entity is Product productEntity)
            {
                entry.Property(NormalizedArabicName.PropertyName).CurrentValue =
                    ArabicSearchText.Normalize(productEntity.ArabicName);
            }
        }
    }
}
