namespace Finance.Tracking.Models;

public class AccountConfiguration
{
    public double Cash { get; set; }
    public Dictionary<string, Holding> Holdings { get; set; } = new();
}