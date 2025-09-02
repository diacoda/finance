using System.Text.Json;
using Finance.Tracking.Models.Yahoo;

namespace Finance.Tracking.Services;

public class YahooService : IYahooService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public YahooService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<double> GetPrice(string symbol, DateOnly? asOf = null)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        // Use today if no date provided
        var targetDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        string range = PickRange(targetDate);

        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range={range}";

        var json = await client.GetStringAsync(url);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Response? data = JsonSerializer.Deserialize<Response>(json, options);
        if (data == null || data.chart == null || data.chart.result == null || data.chart.result.Length == 0)
        {
            throw new InvalidOperationException("Failed to deserialize Yahoo Finance response.");
        }

        Result? result = data.chart.result[0];
        if (result == null || result.timestamp == null || result.indicators == null || result.indicators.quote == null || result.indicators.quote.Length == 0)
        {
            throw new InvalidOperationException("Incomplete data in Yahoo Finance response.");
        }
        long[]? timestamps = result.timestamp;

        double[]? closes = result.indicators.quote[0].close?
            .Select(c => Math.Round(c, 2))
            .ToArray();

        if (timestamps == null || closes == null || timestamps.Length != closes.Length)
        {
            throw new InvalidOperationException("Mismatched timestamp and close data in Yahoo Finance response.");
        }
        if (timestamps.Length == 0)
        {
            throw new InvalidOperationException("No data points found in Yahoo Finance response.");
        }
        if (closes.All(c => c == 0.0))
        {
            throw new InvalidOperationException("All close prices are zero in Yahoo Finance response.");
        }
        // Walk through timestamps, looking for matching date
        for (int i = 0; i < timestamps.Length; i++)
        {
            var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).DateTime);

            if (date == targetDate)
            {
                if (closes[i] != 0.0)
                    return closes[i];
            }
        }

        // If no exact match, return latest available close
        return closes.Last(c => c != 0.0);
    }

    private string PickRange(DateOnly targetDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int days = today.DayNumber - targetDate.DayNumber;

        if (days <= 0) return "1d";
        if (days <= 5) return "5d";
        if (days <= 30) return "1mo";
        if (days <= 90) return "3mo";
        if (days <= 180) return "6mo";
        if (days <= 365) return "1y";
        return "max";
    }
}