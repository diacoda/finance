namespace Finance.Tracking.DTO;

public class GroupedMarketValueDTO<TKey>
{
    public string Name { get; set; } = "";
    public TKey Key { get; set; } = default!;
    public double Total { get; set; }
}
