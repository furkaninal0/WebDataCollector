using ClosedXML.Excel;
using WebDataCollector.Models;
using System.IO;

namespace WebDataCollector.Services;

public class ExcelExportService
{
    private const int ExcelCellLimit = 32767;
    private const int SafeCellLimit = 32000;

    public string Export(List<ScrapeResult> newResults)
    {
        var outputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "INALCODE",
            "WebDataCollector",
            "Outputs"
        );

        Directory.CreateDirectory(outputFolder);

        var masterFilePath = Path.Combine(outputFolder, "master_output.xlsx");

        var allResults = LoadExistingResults(masterFilePath);

        foreach (var newItem in newResults)
        {
            var existing = allResults.FirstOrDefault(x =>
                x.Url.Equals(newItem.Url, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                allResults.Add(newItem);
            }
            else
            {
                existing.IsSuccess = newItem.IsSuccess;
                existing.Status = newItem.Status;
                existing.QualityScore = newItem.QualityScore;
                existing.Sector = newItem.Sector;
                existing.CompanyName = newItem.CompanyName;
                existing.Email = newItem.Email;
                existing.Phone = newItem.Phone;
                existing.Fax = newItem.Fax;
                existing.City = newItem.City;
                existing.Address = newItem.Address;
                existing.WhatsApp = newItem.WhatsApp;
                existing.Instagram = newItem.Instagram;
                existing.Facebook = newItem.Facebook;
                existing.LinkedIn = newItem.LinkedIn;
                existing.Twitter = newItem.Twitter;
                existing.YouTube = newItem.YouTube;
                existing.TikTok = newItem.TikTok;
                existing.Telegram = newItem.Telegram;
                existing.Pinterest = newItem.Pinterest;
                existing.Threads = newItem.Threads;
                existing.Discord = newItem.Discord;
                existing.Medium = newItem.Medium;
                existing.GitHub = newItem.GitHub;
                existing.ErrorMessage = newItem.ErrorMessage;
                existing.HtmlContent = newItem.HtmlContent;
                existing.ScrapedAt = DateTime.Now;
                existing.Emails = newItem.Emails;
                existing.Phones = newItem.Phones;
                existing.Faxes = newItem.Faxes;
                existing.Cities = newItem.Cities;
                existing.Addresses = newItem.Addresses;
                existing.VisitedPages = newItem.VisitedPages;
            }
        }

        return SaveResults(allResults, masterFilePath);
    }

    private List<ScrapeResult> LoadExistingResults(string filePath)
    {
        var results = new List<ScrapeResult>();

        if (!File.Exists(filePath))
            return results;

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet("Results");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var url = worksheet.Cell(row, 1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(url))
                continue;

            var item = new ScrapeResult
            {
                Url = url,
                CompanyName = worksheet.Cell(row, 2).GetString(),
                Email = worksheet.Cell(row, 3).GetString(),
                Phone = worksheet.Cell(row, 4).GetString(),
                Fax = worksheet.Cell(row, 5).GetString(),
                City = worksheet.Cell(row, 6).GetString(),
                Address = worksheet.Cell(row, 7).GetString(),
                WhatsApp = worksheet.Cell(row, 8).GetString(),
                Instagram = worksheet.Cell(row, 9).GetString(),
                Facebook = worksheet.Cell(row, 10).GetString(),
                LinkedIn = worksheet.Cell(row, 11).GetString(),
                Twitter = worksheet.Cell(row, 12).GetString(),
                YouTube = worksheet.Cell(row, 13).GetString(),
                TikTok = worksheet.Cell(row, 14).GetString(),
                Telegram = worksheet.Cell(row, 15).GetString(),
                Pinterest = worksheet.Cell(row, 16).GetString(),
                Threads = worksheet.Cell(row, 17).GetString(),
                Discord = worksheet.Cell(row, 18).GetString(),
                Medium = worksheet.Cell(row, 19).GetString(),
                GitHub = worksheet.Cell(row, 20).GetString(),
                Emails = SplitCellValue(worksheet.Cell(row, 21).GetString()),
                Phones = SplitCellValue(worksheet.Cell(row, 22).GetString()),
                Faxes = SplitCellValue(worksheet.Cell(row, 23).GetString()),
                Cities = SplitCellValue(worksheet.Cell(row, 24).GetString()),
                Addresses = SplitCellValue(worksheet.Cell(row, 25).GetString()),
                VisitedPages = SplitCellValue(worksheet.Cell(row, 26).GetString()),
                ErrorMessage = worksheet.Cell(row, 28).GetString(),
                Sector = worksheet.Cell(row, 29).GetString()
            };

            if (bool.TryParse(worksheet.Cell(row, 27).GetString(), out var isSuccess))
                item.IsSuccess = isSuccess;

            if (int.TryParse(worksheet.Cell(row, 30).GetString(), out var score))
                item.QualityScore = score;

            if (Enum.TryParse<ScrapeStatus>(worksheet.Cell(row, 31).GetString(), out var status))
                item.Status = status;

            if (DateTime.TryParse(worksheet.Cell(row, 32).GetString(), out var scrapedAt))
                item.ScrapedAt = scrapedAt;

            results.Add(item);
        }

        return results;
    }

    private string SaveResults(List<ScrapeResult> results, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Results");

        string[] headers =
        {
            "URL", "Firma Adı", "Ana Email", "Ana Telefon", "Ana Fax", "Ana Şehir", "Ana Adres",
            "WhatsApp", "Instagram", "Facebook", "LinkedIn", "Twitter/X", "YouTube", "TikTok",
            "Telegram", "Pinterest", "Threads", "Discord", "Medium", "GitHub",
            "Bulunan Emailler", "Bulunan Telefonlar", "Bulunan Faxlar", "Bulunan Şehirler",
            "Bulunan Adresler", "Gezilen Sayfalar", "Başarılı", "Hata", "Sektör",
            "Kalite Skoru", "Durum", "Tarama Tarihi"
        };

        for (int i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        worksheet.Range(1, 1, 1, 32).Style.Font.Bold = true;

        int row = 2;

        foreach (var item in results)
        {
            SetCell(worksheet, row, 1, item.Url);
            SetCell(worksheet, row, 2, item.CompanyName);
            SetCell(worksheet, row, 3, item.Email);
            SetCell(worksheet, row, 4, item.Phone);
            SetCell(worksheet, row, 5, item.Fax);
            SetCell(worksheet, row, 6, item.City);
            SetCell(worksheet, row, 7, item.Address);
            SetCell(worksheet, row, 8, item.WhatsApp);
            SetCell(worksheet, row, 9, item.Instagram);
            SetCell(worksheet, row, 10, item.Facebook);
            SetCell(worksheet, row, 11, item.LinkedIn);
            SetCell(worksheet, row, 12, item.Twitter);
            SetCell(worksheet, row, 13, item.YouTube);
            SetCell(worksheet, row, 14, item.TikTok);
            SetCell(worksheet, row, 15, item.Telegram);
            SetCell(worksheet, row, 16, item.Pinterest);
            SetCell(worksheet, row, 17, item.Threads);
            SetCell(worksheet, row, 18, item.Discord);
            SetCell(worksheet, row, 19, item.Medium);
            SetCell(worksheet, row, 20, item.GitHub);
            SetCell(worksheet, row, 21, JoinSafe(item.Emails));
            SetCell(worksheet, row, 22, JoinSafe(item.Phones));
            SetCell(worksheet, row, 23, JoinSafe(item.Faxes));
            SetCell(worksheet, row, 24, JoinSafe(item.Cities));
            SetCell(worksheet, row, 25, JoinSafe(item.Addresses));
            SetCell(worksheet, row, 26, JoinSafe(item.VisitedPages));
            worksheet.Cell(row, 27).Value = item.IsSuccess;
            SetCell(worksheet, row, 28, item.ErrorMessage);
            SetCell(worksheet, row, 29, item.Sector);
            worksheet.Cell(row, 30).Value = item.QualityScore;
            SetCell(worksheet, row, 31, item.Status.ToString());
            SetCell(worksheet, row, 32, item.ScrapedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            row++;
        }

        worksheet.Columns().AdjustToContents();

        try
        {
            workbook.SaveAs(filePath);
            return filePath;
        }
        catch (IOException)
        {
            var fallbackFileName = Path.GetFileNameWithoutExtension(filePath) +
                                   "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                                   ".xlsx";

            var fallbackPath = Path.Combine(Path.GetDirectoryName(filePath)!, fallbackFileName);

            workbook.SaveAs(fallbackPath);
            return fallbackPath;
        }
    }

    private static void SetCell(IXLWorksheet worksheet, int row, int column, string? value)
    {
        worksheet.Cell(row, column).Value = TruncateForExcel(value);
    }

    private static string JoinSafe(IEnumerable<string>? values)
    {
        if (values == null)
            return string.Empty;

        var distinct = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        if (distinct.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        int currentLength = 0;

        foreach (var item in distinct)
        {
            var separatorLength = parts.Count == 0 ? 0 : 3;

            if (currentLength + separatorLength + item.Length > SafeCellLimit)
            {
                parts.Add("... [TRUNCATED]");
                break;
            }

            parts.Add(item);
            currentLength += separatorLength + item.Length;
        }

        return string.Join(" | ", parts);
    }

    private static List<string> SplitCellValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new();

        return value.Split(" | ", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct()
            .ToList();
    }

    private static string TruncateForExcel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        if (value.Length <= ExcelCellLimit)
            return value;

        return value[..SafeCellLimit] + "... [TRUNCATED]";
    }
}