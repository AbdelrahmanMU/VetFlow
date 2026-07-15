using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.Domain.Tests.Catalog;

public sealed class ProductCapabilitiesTests
{
    [Fact]
    public void Open_expiration_requires_its_period_BR_CAT_036()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            new ProductCapabilities(false, false, true, hasOpenExpiration: true, openExpirationPeriod: null));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.OpenExpirationPeriodRequired);
    }

    [Fact]
    public void Open_expiration_period_must_be_positive_BR_CAT_036()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            new ProductCapabilities(false, false, true, hasOpenExpiration: true, openExpirationPeriod: TimeSpan.Zero));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.OpenExpirationPeriodRequired);
    }

    [Fact]
    public void Open_expiration_period_without_the_capability_is_rejected_BR_CAT_036()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            new ProductCapabilities(false, false, true, hasOpenExpiration: false, openExpirationPeriod: TimeSpan.FromDays(30)));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.OpenExpirationPeriodRequired);
    }

    [Theory]
    [InlineData(30 * 24)] // ٣٠ يومًا
    [InlineData(48)] // ٤٨ ساعة
    public void Open_expiration_period_is_stored_when_the_capability_is_enabled_BR_CAT_036(int hours)
    {
        var capabilities = new ProductCapabilities(
            false, false, true, hasOpenExpiration: true, openExpirationPeriod: TimeSpan.FromHours(hours));

        capabilities.OpenExpirationPeriod.ShouldBe(TimeSpan.FromHours(hours));
    }

    [Fact]
    public void Splittable_is_a_free_product_level_capability_BR_CAT_031()
    {
        var capabilities = new ProductCapabilities(
            isSplittable: true, isRefrigerated: false, hasExpiration: false, hasOpenExpiration: false, openExpirationPeriod: null);

        capabilities.IsSplittable.ShouldBeTrue();
    }
}
