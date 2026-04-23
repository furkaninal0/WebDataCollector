using WebDataCollector.Helpers;
using WebDataCollector.Models;

namespace WebDataCollector.Services;

public class ScraperService
{
    private readonly HttpFetchService _httpFetchService;
    private readonly LinkFinderService _linkFinder;
    private readonly AdvancedDataExtractor _advanced;
    private readonly DataQualityService _qualityService;
    private readonly SectorDetectionService _sectorService;
    private readonly AddressExtractorService _addressExtractor;

    public ScraperService()
    {
        _httpFetchService = new HttpFetchService();
        _linkFinder = new LinkFinderService();
        _advanced = new AdvancedDataExtractor();
        _qualityService = new DataQualityService();
        _sectorService = new SectorDetectionService();
        _addressExtractor = new AddressExtractorService();
    }

    public async Task<ScrapeResult> ScrapeAsync(string url, ScrapeOptions options)
    {
        var result = new ScrapeResult { Url = url };

        try
        {
            var homeHtml = await _httpFetchService.GetHtmlWithRetryAsync(url);

            result.IsSuccess = true;
            result.HtmlContent = homeHtml;
            result.VisitedPages.Add(url);

            FillAdvanced(result, homeHtml, options);

            var emails = new List<string>();
            var phones = new List<string>();
            var faxes = new List<string>();
            var cities = new List<string>();
            var addresses = new List<string>();

            Collect(homeHtml, emails, phones, faxes, cities, addresses, options);

            var links = _linkFinder.FindImportantLinks(url, homeHtml, options.MaxImportantLinks);

            foreach (var link in links)
            {
                try
                {
                    await Task.Delay(Math.Max(0, options.RequestDelayMs / 2));

                    var subHtml = await _httpFetchService.GetHtmlWithRetryAsync(link, 2);
                    result.VisitedPages.Add(link);

                    Collect(subHtml, emails, phones, faxes, cities, addresses, options);
                    FillAdvanced(result, subHtml, options);
                }
                catch
                {
                }
            }

            if (options.SearchEmail)
            {
                result.Emails = emails
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                result.Email = SelectBestEmail(result.Emails);
            }

            if (options.SearchPhone)
            {
                result.Phones = phones
                    .Distinct()
                    .ToList();

                result.Phone = SelectBestPhone(result.Phones);
            }

            if (options.SearchFax)
            {
                result.Faxes = faxes
                    .Distinct()
                    .ToList();

                result.Fax = result.Faxes.FirstOrDefault();
            }

            if (options.SearchCity)
            {
                result.Cities = cities
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                result.City = SelectBestCity(result.Cities);
            }

            if (options.SearchAddress)
            {
                result.Addresses = addresses
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                result.Address = SelectBestAddress(result.Addresses);
            }

            result.Sector = _sectorService.DetectSector(result.HtmlContent, result.CompanyName, result.Url);
            result.QualityScore = _qualityService.CalculateScore(result);
            result.Status = _qualityService.CalculateStatus(result);

            if (options.MinQualityScore > 0 && result.QualityScore < options.MinQualityScore)
            {
                result.Status = result.QualityScore == 0
                    ? ScrapeStatus.Failed
                    : ScrapeStatus.PartialSuccess;
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Sector = "Belirsiz";
            result.QualityScore = 0;
            result.Status = _qualityService.CalculateStatus(result);
        }

        return result;
    }

    private void FillAdvanced(ScrapeResult r, string html, ScrapeOptions options)
    {
        r.CompanyName ??= _advanced.ExtractCompanyName(html);

        if (options.SearchWhatsApp)
            r.WhatsApp ??= _advanced.ExtractWhatsApp(html);

        if (options.SearchSocialMedia)
        {
            var s = _advanced.ExtractSocialLinks(html);

            r.Instagram ??= s.Instagram;
            r.Facebook ??= s.Facebook;
            r.LinkedIn ??= s.LinkedIn;
            r.Twitter ??= s.Twitter;
            r.YouTube ??= s.YouTube;
            r.TikTok ??= s.TikTok;
            r.Telegram ??= s.Telegram;
            r.Pinterest ??= s.Pinterest;
            r.Threads ??= s.Threads;
            r.Discord ??= s.Discord;
            r.Medium ??= s.Medium;
            r.GitHub ??= s.GitHub;
        }
    }

    private void Collect(
        string html,
        List<string> emails,
        List<string> phones,
        List<string> faxes,
        List<string> cities,
        List<string> addresses,
        ScrapeOptions options)
    {
        if (options.SearchEmail)
            emails.AddRange(RegexHelper.ExtractEmails(html));

        if (options.SearchPhone)
            phones.AddRange(RegexHelper.ExtractPhones(html));

        if (options.SearchFax)
            faxes.AddRange(RegexHelper.ExtractFaxes(html));

        if (options.SearchCity)
            cities.AddRange(RegexHelper.ExtractCities(html));

        if (options.SearchAddress)
            addresses.AddRange(_addressExtractor.ExtractAddresses(html));
    }

    private static string? SelectBestEmail(List<string> emails)
    {
        if (emails.Count == 0)
            return null;

        return emails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(x =>
                x.StartsWith("info@", StringComparison.OrdinalIgnoreCase) ||
                x.StartsWith("iletisim@", StringComparison.OrdinalIgnoreCase) ||
                x.StartsWith("contact@", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Length)
            .FirstOrDefault();
    }

    private static string? SelectBestPhone(List<string> phones)
    {
        if (phones.Count == 0)
            return null;

        return phones
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(x =>
                x.StartsWith("+90") || x.StartsWith("905") || x.StartsWith("05"))
            .ThenByDescending(x => x.Length)
            .FirstOrDefault();
    }

    private static string? SelectBestCity(List<string> cities)
    {
        return cities
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    private static string? SelectBestAddress(List<string> addresses)
    {
        if (addresses.Count == 0)
            return null;

        return addresses
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderByDescending(IsCleanAddressCandidate)
            .ThenByDescending(x => x.Length)
            .FirstOrDefault();
    }

    private static bool IsCleanAddressCandidate(string address)
    {
        var lower = address.ToLowerInvariant();

        var badTokens = new[]
        {
            " email ", " e ", " fax ", " f ", " tel ", " telefon ", " phone ",
            "instagram", "linkedin", "facebook", "@"
        };

        return !badTokens.Any(lower.Contains);
    }
}