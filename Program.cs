using WebDataCollector.Models;
using WebDataCollector.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var urlReaderService = new UrlReaderService();
var scraperService = new ScraperService();
var excelExportService = new ExcelExportService();

var options = new ScrapeOptions
{
    SearchEmail = true,
    SearchPhone = true,
    SearchFax = true,
    SearchCity = true,
    SearchAddress = true,
    SearchWhatsApp = true,
    SearchSocialMedia = true,
    MinQualityScore = 0,
    MaxImportantLinks = 6,
    RequestDelayMs = 1200,
    IncludeFailedResults = true,
    UpdateExistingRecords = true
};

var urls = await urlReaderService.ReadUrlsAsync("Data/urls.txt");

if (urls.Count == 0)
{
    Console.WriteLine("URL listesi boş veya okunamadı.");
    return;
}

var results = new List<ScrapeResult>();

Console.WriteLine($"Toplam {urls.Count} adet URL bulundu.");
Console.WriteLine(new string('-', 100));

foreach (var url in urls)
{
    var result = await scraperService.ScrapeAsync(url, options);

    if (options.IncludeFailedResults || result.IsSuccess)
    {
        results.Add(result);
    }

    Console.WriteLine($"Site             : {result.Url}");
    Console.WriteLine($"Başarılı         : {result.IsSuccess}");
    Console.WriteLine($"Durum            : {result.Status}");
    Console.WriteLine($"Kalite Skoru     : {result.QualityScore}");
    Console.WriteLine($"Sektör           : {result.Sector ?? "-"}");
    Console.WriteLine($"Firma Adı        : {result.CompanyName ?? "-"}");
    Console.WriteLine($"Ana Email        : {result.Email ?? "-"}");
    Console.WriteLine($"Ana Telefon      : {result.Phone ?? "-"}");
    Console.WriteLine($"Ana Fax          : {result.Fax ?? "-"}");
    Console.WriteLine($"Ana Şehir        : {result.City ?? "-"}");
    Console.WriteLine($"Ana Adres        : {result.Address ?? "-"}");
    Console.WriteLine($"WhatsApp         : {result.WhatsApp ?? "-"}");
    Console.WriteLine($"Instagram        : {result.Instagram ?? "-"}");
    Console.WriteLine($"Facebook         : {result.Facebook ?? "-"}");
    Console.WriteLine($"LinkedIn         : {result.LinkedIn ?? "-"}");
    Console.WriteLine($"Twitter/X        : {result.Twitter ?? "-"}");
    Console.WriteLine($"YouTube          : {result.YouTube ?? "-"}");
    Console.WriteLine($"TikTok           : {result.TikTok ?? "-"}");
    Console.WriteLine($"Telegram         : {result.Telegram ?? "-"}");
    Console.WriteLine($"Pinterest        : {result.Pinterest ?? "-"}");
    Console.WriteLine($"Threads          : {result.Threads ?? "-"}");
    Console.WriteLine($"Discord          : {result.Discord ?? "-"}");
    Console.WriteLine($"Medium           : {result.Medium ?? "-"}");
    Console.WriteLine($"GitHub           : {result.GitHub ?? "-"}");
    Console.WriteLine($"Gezilen Sayfa    : {result.VisitedPages.Count}");
    Console.WriteLine($"Bulunan Email    : {result.Emails.Count}");
    Console.WriteLine($"Bulunan Telefon  : {result.Phones.Count}");
    Console.WriteLine($"Bulunan Fax      : {result.Faxes.Count}");
    Console.WriteLine($"Bulunan Şehir    : {result.Cities.Count}");
    Console.WriteLine($"Bulunan Adres    : {result.Addresses.Count}");
    Console.WriteLine($"Hata             : {result.ErrorMessage ?? "-"}");
    Console.WriteLine(new string('-', 100));

    await Task.Delay(options.RequestDelayMs);
}

excelExportService.Export(results);