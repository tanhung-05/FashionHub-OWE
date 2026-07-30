using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FashionHub.Web.Application.Chat;

public static partial class ChatText
{
    private static readonly string[] SecurityTerms =
    [
        "ignore previous",
        "ignore all",
        "bo qua huong dan",
        "bo qua chi dan",
        "system prompt",
        "api key",
        "connection string",
        "cookie",
        "mat khau",
        "password",
        "du lieu noi bo"
    ];

    public static string Normalize(string value)
    {
        var decomposed = (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return WhitespaceRegex()
            .Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ")
            .Trim();
    }

    public static bool IsSecuritySensitive(string message)
    {
        var normalized = Normalize(message);
        return SecurityTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    public static bool LooksLikeSensitiveAiOutput(string message)
    {
        var normalized = Normalize(message);
        return ApiKeyRegex().IsMatch(message)
            || normalized.Contains("connectionstrings:", StringComparison.Ordinal)
            || normalized.Contains("geminiai:apikey", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"AIza[0-9A-Za-z_-]{20,}")]
    private static partial Regex ApiKeyRegex();
}
