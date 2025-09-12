namespace Finance.Tracking.Interfaces;

public interface IYahooService
{
    public Task<double> GetPrice(string symbol, DateOnly? asOf = null);
}