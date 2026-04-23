namespace WebDataCollector.Services;

public class SectorDetectionService
{
    public string DetectSector(string? html, string? companyName, string? url)
    {
        var text = $"{companyName} {url} {html}".ToLowerInvariant();

        if (ContainsAny(text, "e-ticaret", "sepet", "checkout", "cart", "ürün", "product", "shop"))
            return "E-Ticaret";

        if (ContainsAny(text, "academy", "akademi", "kurs", "course", "lesson", "eğitim"))
            return "Eğitim";

        if (ContainsAny(text, "agency", "ajans", "seo", "software", "yazılım", "web tasarım"))
            return "Ajans / Yazılım";

        if (ContainsAny(text, "clinic", "hospital", "medical", "sağlık", "tedavi", "psikolog", "hekim"))
            return "Sağlık";

        if (ContainsAny(text, "restaurant", "cafe", "kahve", "burger", "menü", "sipariş"))
            return "Yeme-İçme";

        if (ContainsAny(text, "law", "hukuk", "avukat", "attorney"))
            return "Hukuk";

        if (ContainsAny(text, "blog", "haber", "news", "makale", "media"))
            return "İçerik / Medya";

        return "Belirsiz";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }
}