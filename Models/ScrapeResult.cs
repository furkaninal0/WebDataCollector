namespace WebDataCollector.Models;

public class ScrapeResult
{
    public string Url { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

    public ScrapeStatus Status { get; set; } = ScrapeStatus.Failed;
    public int QualityScore { get; set; }
    public string? Sector { get; set; }

    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? WhatsApp { get; set; }

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

    public string? ErrorMessage { get; set; }
    public string? HtmlContent { get; set; }

    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public List<string> Faxes { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public List<string> Addresses { get; set; } = new();
    public List<string> VisitedPages { get; set; } = new();

    public DateTime ScrapedAt { get; set; } = DateTime.Now;
}