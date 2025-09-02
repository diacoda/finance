namespace Finance.Tracking.DTO;

public class PriceDTO
{
    public Symbol Symbol { get; set; }
    public DateOnly Date { get; set; }
    public double Value { get; set; }
}