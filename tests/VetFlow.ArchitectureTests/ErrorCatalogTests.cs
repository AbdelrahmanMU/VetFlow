using System.Globalization;
using System.Reflection;
using Shouldly;
using VetFlow.Api.Errors;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.ArchitectureTests;

/// <summary>
/// Error Catalog integrity (ADR-0018, STD-BE-032 … STD-BE-034): every declared
/// error code is registered exactly once and has an Arabic and an English
/// message resource.
/// </summary>
public sealed class ErrorCatalogTests
{
    private static readonly CultureInfo Arabic = new("ar");
    private static readonly CultureInfo English = new("en");

    public static TheoryData<string> DeclaredErrorCodes()
    {
        var data = new TheoryData<string>();
        foreach (var code in CollectConstants(typeof(CommonErrorCodes)).Concat(CollectConstants(typeof(CatalogErrorCodes))))
        {
            data.Add(code);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DeclaredErrorCodes))]
    public void Every_declared_error_code_is_registered_in_the_catalog_STD_BE_033(string code)
    {
        ErrorCatalog.All.ShouldContain(entry => entry.Code == code);
    }

    [Theory]
    [MemberData(nameof(DeclaredErrorCodes))]
    public void Every_error_code_has_arabic_and_english_messages_STD_BE_034(string code)
    {
        ErrorMessages.Get(code, Arabic).ShouldNotBe(code);
        ErrorMessages.Get(code, English).ShouldNotBe(code);
    }

    public static TheoryData<string> ValidationMessageKeys()
    {
        var data = new TheoryData<string>();
        foreach (var key in CollectConstants(typeof(VetFlow.Application.Common.ValidationMessageKeys)))
        {
            data.Add(key);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ValidationMessageKeys))]
    public void Every_validation_message_key_has_arabic_and_english_text_STD_BE_034(string key)
    {
        // ErrorCatalogTests guards VTF- codes; validation.* keys are the other half
        // of the ar/en localization gate and must not silently fall back to the key.
        ErrorMessages.Get(key, Arabic).ShouldNotBe(key);
        ErrorMessages.Get(key, English).ShouldNotBe(key);
    }

    [Fact]
    public void Error_codes_follow_the_vtf_module_nnn_format_STD_BE_032()
    {
        foreach (var entry in ErrorCatalog.All)
        {
            entry.Code.ShouldMatch(@"^VTF-[A-Z]+-\d{3}$");
        }
    }

    [Fact]
    public void Error_codes_are_unique_repository_wide_STD_BE_032()
    {
        var codes = ErrorCatalog.All.Select(entry => entry.Code).ToList();
        codes.Count.ShouldBe(codes.Distinct(StringComparer.Ordinal).Count());
    }

    private static IEnumerable<string> CollectConstants(Type constantsType) =>
        constantsType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .Select(field => (string?)field.GetRawConstantValue())
            .Where(value => value is not null)
            .Select(value => value ?? string.Empty);
}
