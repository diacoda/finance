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
    public Dictionary<Symbol, Holding> Holdings { get; set; } = new();

    public Account()
    {
        // Initialize Holdings to avoid null reference exceptions
        Holdings = new Dictionary<Symbol, Holding>();
    }

    public bool IsRESP => Type == AccountType.RESP;
    public double MarketValue { get; set; }
}
