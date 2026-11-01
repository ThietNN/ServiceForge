using System.Globalization;

namespace ServiceForge.Application.Validation;

internal static class Iso8601TimestampParser
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mmzzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"
    ];

    private static readonly string[] UtcFormats =
    [
        "yyyy-MM-dd'T'HH:mm'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
    ];

    public static bool TryParse(string? value, out DateTimeOffset parsed)
    {
        if (DateTimeOffset.TryParseExact(
            value,
            UtcFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out parsed))
        {
            return true;
        }

        return DateTimeOffset.TryParseExact(
            value,
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed);
    }
}
