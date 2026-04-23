using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebDataCollector.Services;

public class AddressExtractorService
{
    public List<string> ExtractAddresses(string html)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(html))
            return results;

        results.AddRange(ExtractFromJsonLd(html));
        results.AddRange(ExtractFromAddressTags(html));
        results.AddRange(ExtractFromContactBlocks(html));
        results.AddRange(ExtractByRegexFallback(html));

        return results
            .Select(CleanAddress)
            .Where(IsValidAddressCandidate)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> ExtractFromJsonLd(string html)
    {
        var results = new List<string>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes == null)
            return results;

        foreach (var script in scriptNodes)
        {
            var json = HtmlEntity.DeEntitize(script.InnerText?.Trim() ?? "");
            if (string.IsNullOrWhiteSpace(json))
                continue;

            try
            {
                using var document = JsonDocument.Parse(json);
                TraverseJson(document.RootElement, results);
            }
            catch
            {
            }
        }

        return results;
    }

    private void TraverseJson(JsonElement element, List<string> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    string? street = null;
                    string? locality = null;
                    string? region = null;
                    string? postalCode = null;
                    string? country = null;

                    foreach (var prop in element.EnumerateObject())
                    {
                        var name = prop.Name.ToLowerInvariant();

                        if (name == "streetaddress")
                            street = prop.Value.ToString();
                        else if (name == "addresslocality")
                            locality = prop.Value.ToString();
                        else if (name == "addressregion")
                            region = prop.Value.ToString();
                        else if (name == "postalcode")
                            postalCode = prop.Value.ToString();
                        else if (name == "addresscountry")
                            country = prop.Value.ToString();

                        TraverseJson(prop.Value, results);
                    }

                    var combined = string.Join(", ",
                        new[] { street, locality, region, postalCode, country }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                    if (!string.IsNullOrWhiteSpace(combined))
                        results.Add(combined);

                    break;
                }

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    TraverseJson(item, results);
                break;
        }
    }

    private List<string> ExtractFromAddressTags(string html)
    {
        var results = new List<string>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//address");
        if (nodes == null)
            return results;

        foreach (var node in nodes)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText ?? "");
            if (!string.IsNullOrWhiteSpace(text))
                results.Add(text);
        }

        return results;
    }

    private List<string> ExtractFromContactBlocks(string html)
    {
        var results = new List<string>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var xpath =
            "//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'adres') " +
            "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'address') " +
            "or contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'adres') " +
            "or contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'address') " +
            "or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'contact') " +
            "or contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZÇĞİÖŞÜ','abcdefghijklmnopqrstuvwxyzçğiöşü'),'contact')]";

        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes == null)
            return results;

        foreach (var node in nodes)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText ?? "");
            if (!string.IsNullOrWhiteSpace(text))
                results.Add(text);
        }

        return results;
    }

    private List<string> ExtractByRegexFallback(string html)
    {
        var results = new List<string>();

        var patterns = new[]
        {
            @"(?:adres|address)\s*[:\-]?\s*([^<\n\r]{20,250})",
            @"(?:ofis|office|merkez)\s*[:\-]?\s*([^<\n\r]{20,250})"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                    results.Add(match.Groups[1].Value);
            }
        }

        return results;
    }

    private static string CleanAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = HtmlEntity.DeEntitize(value);
        value = Regex.Replace(value, @"\s+", " ").Trim();

        return value;
    }

    private static bool IsValidAddressCandidate(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        if (address.Length < 15 || address.Length > 220)
            return false;

        var badWords = new[]
        {
            "copyright", "cookie", "javascript", "bootstrap", "instagram",
            "linkedin", "facebook", "twitter", ".js", ".png", ".jpg",
            "placeholder", "required", "target=", "class=", "function("
        };

        if (badWords.Any(x => address.Contains(x, StringComparison.OrdinalIgnoreCase)))
            return false;

        var addressHints = new[]
        {
            "mah", "mahalle", "cad", "caddesi", "sok", "sk", "no", "apt",
            "kat", "daire", "çankaya", "istanbul", "ankara", "izmir", "türkiye"
        };

        if (!addressHints.Any(x => address.Contains(x, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }
}