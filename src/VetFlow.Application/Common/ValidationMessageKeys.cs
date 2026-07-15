namespace VetFlow.Application.Common;

/// <summary>
/// Message resource keys carried by validation failures. Validators and parsers
/// emit keys only; the API middleware is the single translation point
/// (STD-API-012, ADR-0018).
/// </summary>
public static class ValidationMessageKeys
{
    public const string PageMin = "validation.page.min";
    public const string PageMax = "validation.page.max";
    public const string PageSizeRange = "validation.pageSize.range";
    public const string SearchMaxLength = "validation.search.maxLength";
    public const string InvalidId = "validation.id.invalid";
    public const string InvalidBoolean = "validation.boolean.invalid";
    public const string InvalidInteger = "validation.integer.invalid";
    public const string UnknownSortField = "validation.sort.unknown";
    public const string UnknownSortDirection = "validation.dir.unknown";
    public const string UnknownStatus = "validation.status.unknown";
}
