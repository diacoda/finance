namespace Finance.Tracking.Interfaces;

using Finance.Tracking.Models;

public interface IPricingService
{
    public Dictionary<Symbol, double> Prices { get; }

    /// <summary>
    /// Loads prices for all configured symbols. 
    /// If asOf is provided, can later be extended for historical data.
    /// </summary>
    public Task<Dictionary<Symbol, double>> LoadPricesAsync(DateOnly? asOf = null);

    /// <summary>
    /// Get a price for a specific symbol.
    /// </summary>
    public double GetPrice(Symbol symbol);
    public Task<int> SavePriceAsync(Symbol symbol, double price, DateOnly? asOf);
    public Task<int> SavePricesAsync(List<Price> prices);
    Task<List<Price>> GetPricesByDateAsync(DateOnly? asOf = null);
}
