namespace WebDataCollector.Services;

public class HttpFetchService
{
    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; rv:124.0) Gecko/20100101 Firefox/124.0"
    ];

    private readonly Random _random = new();

    public async Task<string> GetHtmlWithRetryAsync(string url, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var client = CreateHttpClient();

            try
            {
                return await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(800 * attempt);
            }
        }

        throw lastException ?? new Exception("HTML alınamadı.");
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgents[_random.Next(UserAgents.Length)]);
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        client.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");

        return client;
    }
}