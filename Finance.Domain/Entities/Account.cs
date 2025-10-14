namespace Finance.Domain.Entities;

using Finance.Domain.ValueObjects;
public sealed class Account
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Owner { get; private set; }
    private readonly List<Holding> _holdings = new();
    public IReadOnlyCollection<Holding> Holdings => _holdings.AsReadOnly();
    public Account(string name, string owner)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }
    public void AddHolding(Holding holding)
    {
        if (holding is null) throw new ArgumentNullException(nameof(holding));
        var existing = _holdings.Find(h => h.Symbol.Equals(holding.Symbol));
        if (existing is null)
            _holdings.Add(holding);
        else
            existing.Increase(holding.Quantity, holding.CostBasis);
    }
    public void RemoveHolding(Symbol symbol)
    {
        var existing = _holdings.Find(h => h.Symbol.Equals(symbol));
        if (existing is null) throw new InvalidOperationException("Holding not found");
        _holdings.Remove(existing);
    }
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("name required");
        Name = newName;
    }
}
