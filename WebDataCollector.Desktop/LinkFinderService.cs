using HtmlAgilityPack;
namespace WebDataCollector.Services;

public class LinkFinderService
{
    private static readonly string[] PriorityKeywords =
    [
        "iletisim",
        "contact",
        "contact-us",
        "contactus",
        "hakkimizda",
        "about",
        "iletişim",
        "kurumsal",
        "sube",
        "şube",
        "ofis",
        "office"
    ];

    public List<string> FindImportantLinks(string baseUrl, string html, int maxCount)
    {
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(html))
            return results;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (linkNodes == null)
            return results;

        foreach (var node in linkNodes)
        {
            var href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrWhiteSpace(href))
                continue;

            var absoluteUrl = ConvertToAbsoluteUrl(baseUrl, href);
            if (string.IsNullOrWhiteSpace(absoluteUrl))
                continue;

            if (PriorityKeywords.Any(k =>
                absoluteUrl.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(absoluteUrl);
            }
        }

        return results
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();
    }

    private static string? ConvertToAbsoluteUrl(string baseUrl, string href)
    {
        try
        {
            if (href.StartsWith("#") ||
                href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var baseUri = new Uri(baseUrl);
            var absoluteUri = new Uri(baseUri, href);

            return absoluteUri.ToString();
        }
        catch
        {
            return null;
        }
    }
}