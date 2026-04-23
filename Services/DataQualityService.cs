using WebDataCollector.Models;

namespace WebDataCollector.Services;

public class DataQualityService
{
    public int CalculateScore(ScrapeResult result)
    {
        int score = 0;

        if (!string.IsNullOrWhiteSpace(result.CompanyName)) score += 10;
        if (!string.IsNullOrWhiteSpace(result.Email)) score += 25;
        if (!string.IsNullOrWhiteSpace(result.Phone)) score += 25;
        if (!string.IsNullOrWhiteSpace(result.WhatsApp)) score += 10;
        if (!string.IsNullOrWhiteSpace(result.Address)) score += 15;
        if (!string.IsNullOrWhiteSpace(result.City)) score += 10;

        if (!string.IsNullOrWhiteSpace(result.Instagram)) score += 3;
        if (!string.IsNullOrWhiteSpace(result.LinkedIn)) score += 4;
        if (!string.IsNullOrWhiteSpace(result.Facebook)) score += 2;
        if (!string.IsNullOrWhiteSpace(result.YouTube)) score += 1;

        if (result.Emails.Count > 1) score += 3;
        if (result.Phones.Count > 1) score += 3;
        if (result.Addresses.Count > 1) score += 2;

        return Math.Min(score, 100);
    }

    public ScrapeStatus CalculateStatus(ScrapeResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            if (result.ErrorMessage.Contains("403") ||
                result.ErrorMessage.Contains("blocked", StringComparison.OrdinalIgnoreCase) ||
                result.ErrorMessage.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            {
                return ScrapeStatus.Blocked;
            }
        }

        var hasEmail = !string.IsNullOrWhiteSpace(result.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(result.Phone);
        var hasWhatsApp = !string.IsNullOrWhiteSpace(result.WhatsApp);
        var hasAddress = !string.IsNullOrWhiteSpace(result.Address);
        var hasCompany = !string.IsNullOrWhiteSpace(result.CompanyName);

        var coreCount = 0;
        if (hasEmail) coreCount++;
        if (hasPhone) coreCount++;
        if (hasWhatsApp) coreCount++;

        var strongCount = 0;
        if (hasEmail) strongCount++;
        if (hasPhone) strongCount++;
        if (hasAddress) strongCount++;
        if (hasCompany) strongCount++;

        if (coreCount >= 2 || (hasEmail && hasAddress) || (hasPhone && hasAddress))
            return ScrapeStatus.Success;

        if (strongCount >= 1)
            return ScrapeStatus.PartialSuccess;

        return result.IsSuccess ? ScrapeStatus.Failed : ScrapeStatus.Failed;
    }

    public string GetFriendlyErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "-";

        if (message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("bilinen böyle bir ana bilgisayar yok", StringComparison.OrdinalIgnoreCase))
        {
            return "DNS bulunamadı / siteye erişilemedi";
        }

        if (message.Contains("403", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return "Erişim engellendi (403)";
        }

        if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "İstek zaman aşımına uğradı";
        }

        if (message.Contains("ssl", StringComparison.OrdinalIgnoreCase))
        {
            return "SSL / sertifika hatası";
        }

        return message;
    }
}