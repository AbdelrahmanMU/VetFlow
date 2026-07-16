using VetFlow.Application.Common;
using VetFlow.Domain.Common;

namespace VetFlow.Api.Endpoints;

/// <summary>
/// Explicit query-string parsing. Malformed values become field errors carrying
/// message resource keys, so every non-2xx response keeps the single canonical
/// validation shape (STD-API-010, STD-API-014) — never a bare binding failure.
/// </summary>
public sealed class QueryStringParser
{
    private readonly Dictionary<string, string[]> _errors = [];

    public Guid? ParseId(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Guid.TryParse(value, out var id))
        {
            return id;
        }

        AddError(field, ValidationMessageKeys.InvalidId);
        return null;
    }

    public bool? ParseBoolean(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        AddError(field, ValidationMessageKeys.InvalidBoolean);
        return null;
    }

    public DateOnly? ParseDate(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        AddError(field, ValidationMessageKeys.InvalidDate);
        return null;
    }

    public int ParseInteger(string? value, string field, int fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        AddError(field, ValidationMessageKeys.InvalidInteger);
        return fallback;
    }

    public TValue ParseToken<TValue>(
        string? value,
        string field,
        IReadOnlyDictionary<string, TValue> tokens,
        TValue fallback,
        string unknownTokenMessageKey)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (tokens.TryGetValue(value.Trim().ToLowerInvariant(), out var parsed))
        {
            return parsed;
        }

        AddError(field, unknownTokenMessageKey);
        return fallback;
    }

    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            throw new ValidationException(_errors);
        }
    }

    private void AddError(string field, string messageKey)
    {
        _errors[field] = _errors.TryGetValue(field, out var existing)
            ? [.. existing, messageKey]
            : [messageKey];
    }
}
