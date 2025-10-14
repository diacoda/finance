using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Finance.Tracking.Services;

public class PricingService : IPricingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private IYahooService _yahooService;
    private FinanceDbContext _dbContext;

    /// <summary>
    /// T.D. e-Series fund URLs for scraping
    /// </summary>
    private static readonly Dictionary<Symbol, string> TdESeriesUrls = new()
    {
        { Symbol.TDB900, "https://ca.investing.com/funds/td-indiciel-canadien-e" },
        { Symbol.TDB902, "https://ca.investing.com/funds/td-us-index-e-cad" },
        { Symbol.TDB911, "https://ca.investing.com/funds/td-international-index-e" }
    };

    /// <summary>
    /// In-memory prices dictionary
    /// </summary>
    public Dictionary<Symbol, double> Prices { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="symbols"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="dbContext"></param>
    /// <param name="yahooService"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public PricingService(IEnumerable<Symbol> symbols, IHttpClientFactory httpClientFactory, FinanceDbContext dbContext, IYahooService yahooService)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _yahooService = yahooService ?? throw new ArgumentNullException(nameof(yahooService));
        Prices = symbols.ToDictionary(s => s, _ => 0.0);
    }

    /// <summary>
    /// Loads price from T.D. e-Series fund page by scraping the HTML.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private async Task LoadTdESeriesPriceAsync(Symbol symbol, string url)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        string html = await client.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var priceNode = doc.DocumentNode.SelectSingleNode("//span[@id='last_last']");
        if (priceNode != null)
        {
            var priceText = priceNode.InnerText.Trim();
            if (double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                Prices[symbol] = price;
            }
        }
        else
        {
            throw new ApplicationException($"Could not find price node for {symbol}. Site structure may have changed.");
        }
    }

    /// <summary>
    /// Loads price from Yahoo Finance via the YahooService.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="asOf"></param>
    /// <returns></returns>
    private async Task LoadYahooPrice(Symbol symbol, System.DateOnly? asOf)
    {
        Prices[symbol] = await _yahooService.GetPrice(symbol.ToTicker(), asOf);
    }

    /// <summary>
    /// Load prices for all symbols, optionally for a specific date.
    /// If asOf is null, uses today's date.
    /// Caches prices in the database to avoid redundant fetches.
    /// Returns a snapshot of the prices dictionary.
    /// </summary>
    /// <param name="asOf"></param>
    /// <returns></returns>
    public async Task<Dictionary<Symbol, double>> LoadPricesAsync(DateOnly? asOf = null)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        // All enum symbols
        var allSymbols = Enum.GetValues<Symbol>();

        // Cached prices from DB
        var cached = _dbContext.Prices
            .Where(p => p.Date == actualDate)
            .ToDictionary(p => p.Symbol, p => p.Value);

        // Find missing or zero-valued symbols
        var missingOrZero = allSymbols
            .Where(s => !cached.TryGetValue(s, out var v) || v == 0.0)
            .ToList();

        // ✅ If everything is cached and valid, return immediately
        if (missingOrZero.Count == 0)
            return Prices = new Dictionary<Symbol, double>(cached);

        // Otherwise, fetch the missing/zero prices
        foreach (var symbol in missingOrZero)
        {
            if (TdESeriesUrls.TryGetValue(symbol, out var url))
                await LoadTdESeriesPriceAsync(symbol, url);
            else
                await LoadYahooPrice(symbol, asOf is null ? null : actualDate);

            var price = Prices[symbol];
            cached[symbol] = price; // update cached dictionary

            _dbContext.Prices.Add(new Models.Price
            {
                Symbol = symbol,
                Date = actualDate,
                Value = price
            });
        }
        await _dbContext.SaveChangesAsync();
        // Merge into Prices
        Prices = new Dictionary<Symbol, double>(cached);
        // return snapshot
        return new Dictionary<Symbol, double>(Prices);
    }

    /// <summary>
    /// Get price for a specific symbol from the in-memory dictionary.
    /// Returns 0.0 if symbol not found.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public double GetPrice(Symbol symbol) =>
        Prices.TryGetValue(symbol, out var price) ? price : 0.0;

    /// <summary>
    /// Saves or updates a single price entry in the database.
    /// If asOf is null, uses today's date.
    /// Returns number of affected rows.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="price"></param>
    /// <param name="asOf"></param>
    /// <returns></returns>
    public async Task<int> SavePriceAsync(Symbol symbol, double price, DateOnly? asOf)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);
        var priceEntry = await _dbContext.Prices
            .FirstOrDefaultAsync(p => p.Symbol == symbol && p.Date == actualDate);

        if (priceEntry != null)
        {
            priceEntry.Value = price;
            _dbContext.Prices.Update(priceEntry);
        }
        else
        {
            _dbContext.Prices.Add(new Models.Price
            {
                Symbol = symbol,
                Date = actualDate,
                Value = price
            });
        }
        return await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Saves or updates multiple price entries in the database.
    /// Returns number of affected rows.
    /// </summary>
    /// <param name="prices"></param>
    /// <returns></returns>
    public async Task<int> SavePricesAsync(List<Price> prices)
    {
        var dates = prices.Select(p => p.Date).Distinct().ToList();
        var symbols = prices.Select(p => p.Symbol).Distinct().ToList();

        // Fetch all existing prices in a single query
        var existingPrices = await _dbContext.Prices
            .Where(p => dates.Contains(p.Date) && symbols.Contains(p.Symbol))
            .ToDictionaryAsync(p => (p.Symbol, p.Date));

        foreach (Price price in prices)
        {
            var key = (price.Symbol, price.Date);
            if (existingPrices.TryGetValue(key, out var existing))
            {
                existing.Value = price.Value;
            }
            else
            {
                _dbContext.Prices.Add(price);
            }
        }
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> SavePrices2Async(List<Price> prices, int batchSize = 500)
    {
        if (prices == null || prices.Count == 0)
            return 0;

        var dates = prices.Select(p => p.Date).Distinct().ToList();
        var symbols = prices.Select(p => p.Symbol).Distinct().ToList();

        // 1. Fetch existing records in one query
        var existingPrices = await _dbContext.Prices
            .Where(p => dates.Contains(p.Date) && symbols.Contains(p.Symbol))
            .ToDictionaryAsync(p => (p.Symbol, p.Date));

        var totalChanges = 0;

        // 2. Process in batches
        foreach (var batch in prices.Chunk(batchSize))
        {
            foreach (var price in batch)
            {
                var key = (price.Symbol, price.Date);
                if (existingPrices.TryGetValue(key, out var existing))
                {
                    existing.Value = price.Value;
                    _dbContext.Prices.Update(existing); // tracked
                }
                else
                {
                    _dbContext.Prices.Add(price);
                    existingPrices[key] = price; // keep dictionary in sync
                }
            }

            totalChanges += await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear(); // ⚡ prevent memory bloat
        }

        return totalChanges;
    }

    /// <summary>
    /// Gets all prices for a specific date.
    /// If asOf is null, defaults to today.
    /// Returns a list of Price objects.
    /// </summary>
    public async Task<List<Price>> GetPricesByDateAsync(DateOnly? asOf = null)
    {
        var actualDate = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        return await _dbContext.Prices
            .Where(p => p.Date == actualDate)
            .OrderBy(p => p.Symbol) // optional, sort by symbol
            .ToListAsync();
    }

}
