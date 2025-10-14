namespace Finance.Tracking.Models;

public class MarketValueGroup
{
    public double Total { get; set; }
    public List<string> AccountNames { get; set; } = new List<string>();
}
