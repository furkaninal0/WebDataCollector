namespace WebDataCollector.Services;

public class UrlReaderService
{
    public async Task<List<string>> ReadUrlsAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<string>();
        }

        var lines = await File.ReadAllLinesAsync(filePath);

        return lines
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}