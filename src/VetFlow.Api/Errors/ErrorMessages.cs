using System.Globalization;
using System.Resources;

namespace VetFlow.Api.Errors;

/// <summary>
/// Localized user-facing messages (ADR-0007): Arabic is the neutral resource,
/// English is a satellite. A missing key falls back to the key itself so a
/// localization gap is visible, never silent.
/// </summary>
public static class ErrorMessages
{
    private static readonly ResourceManager Manager =
        new("VetFlow.Api.Errors.ErrorMessages", typeof(ErrorMessages).Assembly);

    public static string Get(string key, CultureInfo culture) =>
        Manager.GetString(key, culture) ?? key;
}
