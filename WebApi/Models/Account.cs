using System.ComponentModel.DataAnnotations.Schema;
namespace Finance.Tracking.Models;

public class Account
{
    public required string Name { get; set; }
    public required string Owner { get; set; }
    public AccountType Type { get; set; }
    public AccountFilter AccountFilter { get; set; }
    public required Bank Bank { get; set; }
    // Define Currency as an enum for demonstration; replace with your actual Currency type if needed
    public Currency Currency { get; set; }
    public double Cash { get; set; }
    // Change from Dictionary to ICollection<Holding>
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    // Expose a dictionary for fast lookup and convenience (not mapped to DB)
    [NotMapped]
    public Dictionary<Symbol, Holding> HoldingsDict
    {
        get => Holdings.ToDictionary(h => h.Symbol); // Symbol is enum
        set => Holdings = value.Values.ToList();
    }

    public Account()
    {
    }

    public bool IsRESP => Type == AccountType.RESP;
    public double MarketValue { get; set; }


}
