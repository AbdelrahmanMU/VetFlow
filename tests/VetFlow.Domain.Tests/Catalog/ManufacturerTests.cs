using Shouldly;
using VetFlow.Domain.Catalog;

namespace VetFlow.Domain.Tests.Catalog;

/// <summary>
/// The manufacturer aggregate (REQ-CAT-013/048): Arabic-name-only managed value,
/// active on creation (BR-CAT-052), renameable (BR-CAT-007), and reversibly
/// deactivatable (BR-CAT-052 / DEC-CAT-032). Mirrors the category aggregate tests.
/// </summary>
public sealed class ManufacturerTests
{
    [Fact]
    public void A_new_manufacturer_is_active_BR_CAT_052()
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "شركة الأمل");

        manufacturer.IsActive.ShouldBeTrue();
        manufacturer.Name.ShouldBe("شركة الأمل");
    }

    [Fact]
    public void The_constructor_trims_the_name_BR_CAT_007()
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "  شركة الأمل  ");

        manufacturer.Name.ShouldBe("شركة الأمل");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void The_constructor_rejects_an_empty_name_BR_CAT_007(string? name)
    {
        Should.Throw<ArgumentException>(() => new Manufacturer(Guid.NewGuid(), name!));
    }

    [Fact]
    public void Rename_changes_and_trims_the_name_REQ_CAT_013()
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "شركة الأمل");

        manufacturer.Rename("  شركة النور  ");

        manufacturer.Name.ShouldBe("شركة النور");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rename_rejects_an_empty_name_BR_CAT_007(string? name)
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "شركة الأمل");

        Should.Throw<ArgumentException>(() => manufacturer.Rename(name!));
    }

    [Fact]
    public void Deactivate_then_activate_toggles_the_state_REQ_CAT_048()
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "شركة الأمل");

        manufacturer.Deactivate();
        manufacturer.IsActive.ShouldBeFalse();

        manufacturer.Activate();
        manufacturer.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivation_does_not_touch_the_name_DEC_CAT_032()
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), "شركة الأمل");

        manufacturer.Deactivate();

        manufacturer.Name.ShouldBe("شركة الأمل");
    }
}
