using System.Net;
using System.Text.RegularExpressions;

namespace WebDataCollector.Helpers;

public static class RegexHelper
{
    private static readonly string[] TurkeyCities =
    [
        "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Amasya", "Ankara", "Antalya", "Artvin", "Aydın",
        "Balıkesir", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa",
        "Çanakkale", "Çankırı", "Çorum",
        "Denizli", "Diyarbakır",
        "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
        "Gaziantep", "Giresun", "Gümüşhane",
        "Hakkâri", "Hatay",
        "Isparta", "Mersin", "İstanbul", "İzmir",
        "Kars", "Kastamonu", "Kayseri", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya",
        "Malatya", "Manisa", "Kahramanmaraş", "Mardin", "Muğla", "Muş",
        "Nevşehir", "Niğde", "Ordu", "Rize", "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas",
        "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Şanlıurfa", "Uşak", "Van", "Yozgat",
        "Zonguldak", "Aksaray", "Bayburt", "Karaman", "Kırıkkale", "Batman", "Şırnak", "Bartın",
        "Ardahan", "Iğdır", "Yalova", "Karabük", "Kilis", "Osmaniye", "Düzce"
    ];

    public static List<string> ExtractEmails(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new();

        var matches = Regex.Matches(
            html,
            @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
            RegexOptions.IgnoreCase);

        return matches
            .Select(m => m.Value.Trim().ToLowerInvariant())
            .Where(IsValidEmailCandidate)
            .Distinct()
            .ToList();
    }

    public static List<string> ExtractPhones(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new();

        var matches = Regex.Matches(
            html,
            @"(?:\+?\d[\d\s\-\(\)]{8,}\d)",
            RegexOptions.IgnoreCase);

        return matches
            .Select(m => NormalizePhone(m.Value))
            .Where(IsValidPhoneCandidate)
            .Distinct()
            .ToList();
    }

    public static List<string> ExtractFaxes(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new();

        var results = new List<string>();

        var patterns = new[]
        {
            @"(?:fax|faks|telefax)\s*[:\-]?\s*(\+?\d[\d\s\-\(\)]{8,}\d)",
            @"(\+?\d[\d\s\-\(\)]{8,}\d)\s*(?:fax|faks|telefax)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count <= 1) continue;

                var normalized = NormalizePhone(match.Groups[1].Value);
                if (IsValidPhoneCandidate(normalized))
                    results.Add(normalized);
            }
        }

        return results.Distinct().ToList();
    }

    public static List<string> ExtractCities(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new();

        var foundCities = new List<string>();

        foreach (var city in TurkeyCities)
        {
            var pattern = $@"\b{Regex.Escape(city)}\b";
            if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase))
                foundCities.Add(city);
        }

        return foundCities
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizePhone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        value = Regex.Replace(value, @"[^\d+]", "");

        if (value.Count(c => c == '+') > 1)
            return string.Empty;

        if (value.Contains('+') && !value.StartsWith("+"))
            return string.Empty;

        return value;
    }

    private static bool IsValidPhoneCandidate(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 10 || digits.Length > 13)
            return false;

        if (!(digits.StartsWith("05") ||
              digits.StartsWith("5") ||
              digits.StartsWith("90") ||
              digits.StartsWith("021") ||
              digits.StartsWith("022") ||
              digits.StartsWith("023") ||
              digits.StartsWith("024") ||
              digits.StartsWith("031") ||
              digits.StartsWith("032") ||
              digits.StartsWith("033") ||
              digits.StartsWith("034") ||
              digits.StartsWith("035") ||
              digits.StartsWith("036") ||
              digits.StartsWith("037") ||
              digits.StartsWith("038") ||
              digits.StartsWith("039") ||
              digits.StartsWith("04")))
            return false;

        if (digits.Distinct().Count() <= 2)
            return false;

        var obviousGarbagePrefixes = new[]
        {
            "2026", "2025", "1900", "2000", "1111", "1234", "177", "157", "158", "159", "160", "161"
        };

        if (obviousGarbagePrefixes.Any(digits.StartsWith))
            return false;

        return true;
    }

    private static bool IsValidEmailCandidate(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var badParts = new[]
        {
            "example.com", "ornek@", "test@", "demo@", "sample@", "dummy@",
            "noreply@", "no-reply@", "donotreply@", "example@", "mail@example",
            ".png", ".jpg", ".jpeg", ".svg", ".webp"
        };

        if (badParts.Any(x => email.Contains(x, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }
}