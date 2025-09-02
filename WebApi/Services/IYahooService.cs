namespace Finance.Tracking.Services;

public interface IYahooService
{
    public Task<double> GetPrice(string symbol, DateOnly? asOf = null);
}