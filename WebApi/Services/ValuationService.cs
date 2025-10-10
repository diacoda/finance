namespace Finance.Tracking.Services;

public class ValuationService : IValuationService
{
    public double ComputeHoldingValue(Holding holding, IReadOnlyDictionary<Symbol, double> prices)
    {
        if (holding == null) throw new ArgumentNullException(nameof(holding));

        if (holding.Symbol == Symbol.CASH)
            return holding.Quantity;

        if (prices != null && prices.TryGetValue(holding.Symbol, out var price))
            return holding.Quantity * price;

        return 0.0;
    }

    public double ComputeMarketValue(IEnumerable<Holding> holdings, IReadOnlyDictionary<Symbol, double> prices)
    {
        if (holdings == null) throw new ArgumentNullException(nameof(holdings));
        double total = 0.0;
        foreach (var h in holdings)
            total += ComputeHoldingValue(h, prices);
        return total;
    }
}
