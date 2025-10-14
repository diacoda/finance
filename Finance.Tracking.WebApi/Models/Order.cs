namespace Finance.Tracking.Models;

public class Order
{
    public string Account { get; set; } = string.Empty;
    public Symbol Symbol { get; set; } = default!;   // e.g., "VFV.TO"
    public OrderSide Side { get; set; }              // Buy or Sell
    public int Quantity { get; set; }                // Number of shares
    public double Price { get; set; }
    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public override string ToString()
    {
        return $"{Side} {Quantity} {Symbol} @ {Price:F2}";
    }
}