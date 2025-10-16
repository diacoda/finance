namespace Finance.Tracking.Domain.Entities;

public class Price
{
    public Symbol Symbol { get; set; }
    public string SymbolString => Symbol.ToString();
    public DateOnly Date { get; set; }
    public double Value { get; set; }
}
