using HtmlAgilityPack;
using System.Xml;

namespace WebDataCollector.Services;

public class AdvancedDataExtractor
{
    public string? ExtractCompanyName(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var cleaned = Clean(title);

        cleaned = cleaned.Replace("Ana Sayfa -", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("Ana Sayfa –", "", StringComparison.OrdinalIgnoreCase)
                         .Trim(' ', '-', '|', '–');

        return cleaned;
    }

    public SocialLinksResult ExtractSocialLinks(string html)
    {
        var result = new SocialLinksResult();

        if (string.IsNullOrWhiteSpace(html))
            return result;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links == null)
            return result;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrWhiteSpace(href))
                continue;

            var h = href.ToLowerInvariant();

            if (h.Contains("instagram.com"))
                result.Instagram ??= href;

            if (h.Contains("facebook.com"))
                result.Facebook ??= href;

            if (h.Contains("linkedin.com"))
                result.LinkedIn ??= href;

            if (h.Contains("twitter.com") || h.Contains("x.com"))
                result.Twitter ??= href;

            if (h.Contains("youtube.com") || h.Contains("youtu.be"))
                result.YouTube ??= href;

            if (h.Contains("tiktok.com"))
                result.TikTok ??= href;

            if (h.Contains("t.me") || h.Contains("telegram"))
                result.Telegram ??= href;

            if (h.Contains("pinterest.com"))
                result.Pinterest ??= href;

            if (h.Contains("threads.net"))
                result.Threads ??= href;

            if (h.Contains("discord.gg") || h.Contains("discord.com"))
                result.Discord ??= href;

            if (h.Contains("medium.com"))
                result.Medium ??= href;

            if (h.Contains("github.com"))
                result.GitHub ??= href;
        }

        return result;
    }

    public string? ExtractWhatsApp(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links == null)
            return null;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "").Trim();

            if (href.Contains("wa.me", StringComparison.OrdinalIgnoreCase) ||
                href.Contains("whatsapp", StringComparison.OrdinalIgnoreCase) ||
                href.Contains("api.whatsapp.com", StringComparison.OrdinalIgnoreCase))
            {
                return href;
            }
        }

        return null;
    }

    private static string Clean(string text)
    {
        return HtmlEntity.DeEntitize(text)
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();
    }
}

public class SocialLinksResult
{
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? LinkedIn { get; set; }
    public string? Twitter { get; set; }
    public string? YouTube { get; set; }
    public string? TikTok { get; set; }
    public string? Telegram { get; set; }
    public string? Pinterest { get; set; }
    public string? Threads { get; set; }
    public string? Discord { get; set; }
    public string? Medium { get; set; }
    public string? GitHub { get; set; }
}