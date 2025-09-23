namespace Finance.Tracking.Models;

public class Holding
{
    public int Id { get; set; }             // <-- primary key
    public Symbol Symbol { get; set; }
    public double Quantity { get; set; }
    public AssetClass AssetClass => SymbolToAssetClass.Resolve(Symbol);


    public string AccountName { get; set; } = null!;
    public Account Account { get; set; } = null!;


}