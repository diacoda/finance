namespace Finance.Domain.Entities;

using Finance.Domain.ValueObjects;
public sealed class Holding
{
    public Guid Id { get; private set; }
    public Symbol Symbol { get; private set; }
    public decimal Quantity { get; private set; }
    public Money CostBasis { get; private set; }
    public Holding(Symbol symbol, decimal quantity, Money costBasis)
    {
        Id = Guid.NewGuid();
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Quantity = quantity;
        CostBasis = costBasis ?? throw new ArgumentNullException(nameof(costBasis));
    }
    public void Increase(decimal qty, Money additionalCost)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (additionalCost.Currency != CostBasis.Currency) throw new InvalidOperationException("Currency mismatch");
        var totalCost = CostBasis.Amount * Quantity + additionalCost.Amount;
        Quantity += qty;
        CostBasis = new Money(totalCost / Quantity, CostBasis.Currency);
    }
    public void Decrease(decimal qty)
    {
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        if (qty > Quantity) throw new InvalidOperationException("Not enough quantity to reduce");
        Quantity -= qty;
    }
}
