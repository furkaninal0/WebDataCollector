using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using WebDataCollector.Models;
using WebDataCollector.Services;

namespace WebDataCollector.Desktop;

public partial class MainWindow : Window
{
    private bool _isRunning = false;
    private CancellationTokenSource? _cts;
    private readonly LicenseService _licenseService = new();
    private LicenseInfo _currentLicense = new();

    public MainWindow()
    {
        InitializeComponent();

        BtnLoadFile.Click += BtnLoadFile_Click;
        BtnClearUrls.Click += BtnClearUrls_Click;
        BtnStart.Click += BtnStart_Click;
        BtnStop.Click += BtnStop_Click;
        BtnOpenOutputFolder.Click += BtnOpenOutputFolder_Click;
        BtnExport.Click += BtnExport_Click;
        BtnLicensePanel.Click += BtnLicensePanel_Click;
        BtnActivateLicense.Click += BtnActivateLicense_Click;
        BtnCopyMachineCode.Click += BtnCopyMachineCode_Click;

        BtnStop.IsEnabled = false;

        LoadLicenseInfo();
        AppendLog("Uygulama hazır.");
    }

    private void BtnLicensePanel_Click(object sender, RoutedEventArgs e)
    {
        LicensePopup.IsOpen = !LicensePopup.IsOpen;
    }

    private void LoadLicenseInfo()
    {
        _currentLicense = _licenseService.GetCurrentLicense();

        TxtMachineCode.Text = _currentLicense.MachineCode;
        TxtLicensePlan.Text = $"Plan: {_currentLicense.Tier}";
        TxtLicenseUsage.Text = _currentLicense.Tier == LicenseTier.Pro
     ? "Kalan: Sınırsız"
     : $"Kalan: {_currentLicense.RemainingUrlCount} URL";

        TxtStatus.Text = "Hazır";
    }

    private void BtnActivateLicense_Click(object sender, RoutedEventArgs e)
    {
        var key = TxtLicenseKey.Text.Trim();

        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("Lütfen lisans anahtarını girin.", "Uyarı",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var validation = _licenseService.ValidateLicense(key, TxtMachineCode.Text.Trim());

        if (!validation.IsValid)
        {
            MessageBox.Show("Geçersiz lisans anahtarı.", "Hata",
                MessageBoxButton.OK, MessageBoxImage.Error);
            AppendLog("Lisans doğrulanamadı.");
            return;
        }

        _licenseService.SaveLicenseKey(key);
        _currentLicense = validation;

        TxtLicensePlan.Text = $"Plan: {_currentLicense.Tier}";
        TxtLicenseUsage.Text = _currentLicense.Tier == LicenseTier.Pro
            ? "Limit: Sınırsız"
            : $"Limit: {_currentLicense.MaxUrlLimit} URL";

        MessageBox.Show("Lisans başarıyla aktifleştirildi.", "Başarılı",
            MessageBoxButton.OK, MessageBoxImage.Information);

        AppendLog($"Lisans aktifleştirildi: {_currentLicense.Tier}");
    }

    private void BtnCopyMachineCode_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtMachineCode.Text);
        AppendLog("Makine kodu kopyalandı.");
    }

    private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text Files|*.txt;*.csv|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            TxtUrls.Text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            AppendLog($"Dosya yüklendi: {dialog.FileName}");
            UpdateUrlCount();
        }
    }

    private void BtnClearUrls_Click(object sender, RoutedEventArgs e)
    {
        TxtUrls.Clear();
        AppendLog("URL listesi temizlendi.");
        UpdateUrlCount();
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
            return;

        var urls = TxtUrls.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (urls.Count == 0)
        {
            AppendLog("URL listesi boş.");
            return;
        }

        if (_currentLicense.Tier == LicenseTier.Free)
        {
            if (_currentLicense.RemainingUrlCount <= 0)
            {
                MessageBox.Show(
                    "Ücretsiz kullanım hakkınız doldu. Devam etmek için lisans anahtarı girmeniz gerekiyor.",
                    "Lisans Gerekli",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                LicensePopup.IsOpen = true;
                return;
            }

            if (urls.Count > _currentLicense.RemainingUrlCount)
            {
                MessageBox.Show(
                    $"Kalan hakkınız {_currentLicense.RemainingUrlCount} URL. " +
                    $"Daha fazla tarama için lisans anahtarı girmeniz gerekiyor.",
                    "Yetersiz Kalan Hak",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                LicensePopup.IsOpen = true;
                return;
            }
        }

        _isRunning = true;
        _cts = new CancellationTokenSource();

        BtnStart.IsEnabled = false;
        BtnStop.IsEnabled = true;

        TxtSuccessCount.Text = "0";
        TxtFailedCount.Text = "0";
        TxtAverageScore.Text = "0";
        TxtTotalUrls.Text = urls.Count.ToString();
        ProgressScan.Value = 0;
        TxtLog.Clear();

        TxtStatus.Text = "Tarama Yapılıyor";

        var options = new ScrapeOptions
        {
            SearchEmail = ChkEmail.IsChecked == true,
            SearchPhone = ChkPhone.IsChecked == true,
            SearchFax = ChkFax.IsChecked == true,
            SearchCity = ChkCity.IsChecked == true,
            SearchAddress = ChkAddress.IsChecked == true,
            SearchWhatsApp = ChkWhatsApp.IsChecked == true,
            SearchSocialMedia = ChkSocialMedia.IsChecked == true,
            MinQualityScore = int.TryParse(TxtMinScore.Text, out var s) ? s : 0,
            MaxImportantLinks = int.TryParse(TxtMaxLinks.Text, out var l) ? l : 6,
            RequestDelayMs = int.TryParse(TxtDelayMs.Text, out var d) ? d : 1200,
            IncludeFailedResults = ChkIncludeFailed.IsChecked == true,
            UpdateExistingRecords = ChkUpdateExisting.IsChecked == true
        };

        var scraper = new ScraperService();
        var excel = new ExcelExportService();
        var qualityService = new DataQualityService();

        var results = new List<ScrapeResult>();

        int success = 0;
        int failed = 0;
        int totalScore = 0;
        bool cancelled = false;

        AppendLog($"Toplam {urls.Count} URL işlenecek...");
        AppendLog("Tarama başladı.");

        try
        {
            for (int i = 0; i < urls.Count; i++)
            {
                if (_cts!.IsCancellationRequested)
                {
                    AppendLog("İşlem kullanıcı tarafından durduruldu.");
                    TxtStatus.Text = "Durduruldu";
                    cancelled = true;
                    break;
                }

                var url = urls[i];

                AppendLog($"[{i + 1}/{urls.Count}] İşleniyor: {url}");

                if (_currentLicense.Tier == LicenseTier.Free)
                {
                    _currentLicense.UsedUrlCount++;
                    _licenseService.SaveLicenseInfo(_currentLicense);

                    TxtLicenseUsage.Text = $"Kalan: {_currentLicense.RemainingUrlCount} URL";
                }

                var result = await scraper.ScrapeAsync(url, options);
                AppendLog($"DEBUG: result status = {result.Status}");
                AppendLog($"DEBUG: email = {result.Email}");
                AppendLog($"DEBUG: phone = {result.Phone}");

                if (options.IncludeFailedResults || result.IsSuccess)
                {
                    results.Add(result);
                }

                if (result.Status == ScrapeStatus.Success || result.Status == ScrapeStatus.PartialSuccess)
                    success++;
                else
                    failed++;

                totalScore += result.QualityScore;

                TxtSuccessCount.Text = success.ToString();
                TxtFailedCount.Text = failed.ToString();
                TxtAverageScore.Text = (results.Count > 0 ? totalScore / results.Count : 0).ToString();

                ProgressScan.Value = ((i + 1) * 100.0) / urls.Count;

                AppendLog(
                    $"→ Durum: {GetFriendlyStatusText(result.Status)} | " +
                    $"Skor: {result.QualityScore} | " +
                    $"Firma: {ShortenForLog(result.CompanyName, 60)}");

                if (!string.IsNullOrWhiteSpace(result.Email))
                    AppendLog($"   Email   : {ShortenForLog(result.Email, 100)}");

                if (!string.IsNullOrWhiteSpace(result.Phone))
                    AppendLog($"   Telefon : {ShortenForLog(result.Phone, 60)}");

                if (!string.IsNullOrWhiteSpace(result.Fax))
                    AppendLog($"   Fax     : {ShortenForLog(result.Fax, 60)}");

                if (!string.IsNullOrWhiteSpace(result.City))
                    AppendLog($"   Şehir   : {ShortenForLog(result.City, 40)}");

                if (!string.IsNullOrWhiteSpace(result.Address))
                    AppendLog($"   Adres   : {ShortenForLog(result.Address, 140)}");

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    AppendLog($"   Hata    : {ShortenForLog(qualityService.GetFriendlyErrorMessage(result.ErrorMessage), 140)}");
            }

            if (!cancelled)
            {
                var excelPath = excel.Export(results);
                AppendLog($"Excel çıktısı oluşturuldu: {excelPath}");

                if (results.Count == 0)
                {
                    AppendLog("UYARI: Veri bulunamadı ama boş Excel oluşturuldu.");
                }

                TxtStatus.Text = "Tamamlandı";

                MessageBox.Show(
                    $"Tarama tamamlandı.\n\n" +
                    $"Toplam: {urls.Count}\n" +
                    $"Başarılı: {success}\n" +
                    $"Başarısız: {failed}\n" +
                    $"Ortalama Skor: {TxtAverageScore.Text}",
                    "İşlem Bitti",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AppendLog("İşlem tamamlandı.");
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Hata";
            AppendLog($"Beklenmeyen hata: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        AppendLog("Durdurma isteği gönderildi.");
    }

    private void BtnOpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "INALCODE",
            "WebDataCollector",
            "Outputs"
        );

        Directory.CreateDirectory(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

        AppendLog($"Çıktı klasörü açıldı: {path}");
    }
    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var outputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "INALCODE",
            "WebDataCollector",
            "Outputs"
        );

        var path = Path.Combine(outputFolder, "master_output.xlsx");

        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });

            AppendLog("master_output.xlsx açıldı.");
        }
        else
        {
            AppendLog($"master_output.xlsx henüz oluşmadı. Kontrol edilen yer: {path}");
        }
    }
    private void UpdateUrlCount()
    {
        var count = TxtUrls.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Count(x => !string.IsNullOrWhiteSpace(x));

        TxtTotalUrls.Text = count.ToString();
    }

    private void AppendLog(string message)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        TxtLog.ScrollToEnd();
    }

    private static string NormalizeErrorMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "-";

        if (message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("bilinen böyle bir ana bilgisayar yok", StringComparison.OrdinalIgnoreCase))
        {
            return "DNS bulunamadı / alan adına erişilemedi.";
        }

        if (message.Contains("403", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return "Erişim engellendi (403 / Forbidden).";
        }

        if (message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "İstek zaman aşımına uğradı.";
        }

        return message;
    }

    private static string ShortenText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        value = value.Trim();

        if (value.Length <= maxLength)
            return value;

        return value[..maxLength] + "...";
    }

    private string CleanForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " ")
                    .Trim();
    }

    private string ShortenForLog(string? value, int maxLength = 140)
    {
        var cleaned = CleanForLog(value);

        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        if (cleaned.Length <= maxLength)
            return cleaned;

        return cleaned[..maxLength] + "...";
    }

    private string GetFriendlyStatusText(ScrapeStatus status)
    {
        return status switch
        {
            ScrapeStatus.Success => "Başarılı",
            ScrapeStatus.PartialSuccess => "Kısmi Başarılı",
            ScrapeStatus.Blocked => "Engellendi",
            ScrapeStatus.Failed => "Başarısız",
            _ => status.ToString()
        };
    }
}