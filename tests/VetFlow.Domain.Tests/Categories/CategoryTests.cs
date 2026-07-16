using Shouldly;
using VetFlow.Domain.Categories;

namespace VetFlow.Domain.Tests.Categories;

/// <summary>
/// The category aggregate (REQ-CTG-002/003/004): Arabic-name-only managed value,
/// active on creation (BR-CTG-004), renameable (BR-CTG-002), and reversibly
/// deactivatable (BR-CTG-005 / DEC-CTG-002).
/// </summary>
public sealed class CategoryTests
{
    [Fact]
    public void A_new_category_is_active_BR_CTG_004()
    {
        var category = new Category(Guid.NewGuid(), "أدوية");

        category.IsActive.ShouldBeTrue();
        category.Name.ShouldBe("أدوية");
    }

    [Fact]
    public void The_constructor_trims_the_name_BR_CTG_002()
    {
        var category = new Category(Guid.NewGuid(), "  أدوية  ");

        category.Name.ShouldBe("أدوية");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void The_constructor_rejects_an_empty_name_BR_CTG_002(string? name)
    {
        Should.Throw<ArgumentException>(() => new Category(Guid.NewGuid(), name!));
    }

    [Fact]
    public void Rename_changes_and_trims_the_name_REQ_CTG_003()
    {
        var category = new Category(Guid.NewGuid(), "أدوية");

        category.Rename("  أعلاف  ");

        category.Name.ShouldBe("أعلاف");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rename_rejects_an_empty_name_BR_CTG_002(string? name)
    {
        var category = new Category(Guid.NewGuid(), "أدوية");

        Should.Throw<ArgumentException>(() => category.Rename(name!));
    }

    [Fact]
    public void Deactivate_then_activate_toggles_the_state_REQ_CTG_004()
    {
        var category = new Category(Guid.NewGuid(), "أدوية");

        category.Deactivate();
        category.IsActive.ShouldBeFalse();

        category.Activate();
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivation_does_not_touch_the_name_DEC_CTG_002()
    {
        var category = new Category(Guid.NewGuid(), "أدوية");

        category.Deactivate();

        category.Name.ShouldBe("أدوية");
    }
}
