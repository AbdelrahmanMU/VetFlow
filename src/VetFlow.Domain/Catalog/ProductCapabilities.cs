using VetFlow.Domain.Common;

namespace VetFlow.Domain.Catalog;

/// <summary>
/// Product capabilities (overview §5): splittable (BR-CAT-031), refrigerated
/// (BR-CAT-045 display/filter flag), has expiration (BR-CAT-037), and open
/// expiration with its mandatory period (BR-CAT-036).
/// </summary>
public sealed record ProductCapabilities
{
    public ProductCapabilities(
        bool isSplittable,
        bool isRefrigerated,
        bool hasExpiration,
        bool hasOpenExpiration,
        TimeSpan? openExpirationPeriod)
    {
        if (hasOpenExpiration && (openExpirationPeriod is null || openExpirationPeriod <= TimeSpan.Zero))
        {
            throw new BusinessRuleException(CatalogErrorCodes.OpenExpirationPeriodRequired);
        }

        if (!hasOpenExpiration && openExpirationPeriod is not null)
        {
            throw new BusinessRuleException(CatalogErrorCodes.OpenExpirationPeriodRequired);
        }

        IsSplittable = isSplittable;
        IsRefrigerated = isRefrigerated;
        HasExpiration = hasExpiration;
        HasOpenExpiration = hasOpenExpiration;
        OpenExpirationPeriod = openExpirationPeriod;
    }

    public bool IsSplittable { get; }

    public bool IsRefrigerated { get; }

    public bool HasExpiration { get; }

    public bool HasOpenExpiration { get; }

    public TimeSpan? OpenExpirationPeriod { get; }
}
