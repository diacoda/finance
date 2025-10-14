namespace Finance.Tracking.Interfaces;

public interface IValuationService
{
    double ComputeHoldingValue(Holding holding, IReadOnlyDictionary<Symbol, double> prices);
    double ComputeMarketValue(IEnumerable<Holding> holdings, IReadOnlyDictionary<Symbol, double> prices);
}
