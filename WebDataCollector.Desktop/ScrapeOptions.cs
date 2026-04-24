namespace WebDataCollector.Models;

public class ScrapeOptions
{
    public bool SearchEmail { get; set; } = true;
    public bool SearchPhone { get; set; } = true;
    public bool SearchFax { get; set; } = true;
    public bool SearchCity { get; set; } = true;
    public bool SearchAddress { get; set; } = true;
    public bool SearchWhatsApp { get; set; } = true;
    public bool SearchSocialMedia { get; set; } = true;

    public int MinQualityScore { get; set; } = 0;
    public int MaxImportantLinks { get; set; } = 6;
    public int RequestDelayMs { get; set; } = 1200;

    public bool IncludeFailedResults { get; set; } = true;
    public bool UpdateExistingRecords { get; set; } = true;
}