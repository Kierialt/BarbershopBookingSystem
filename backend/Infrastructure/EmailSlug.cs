using System.Globalization;
using System.Text;

namespace backend.Infrastructure;

public static class EmailSlug
{
    public static string BarberLoginLocalPart(string displayName, int barberId)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return $"barber.{barberId}";

        var first = RemoveDiacritics(parts[0]);
        var last = RemoveDiacritics(parts[^1]).ToLowerInvariant();
        if (first.Length == 0 || last.Length == 0)
            return $"barber.{barberId}";

        var initial = char.ToLowerInvariant(first[0]);
        return $"{initial}{last}.{barberId}";
    }

    public static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
