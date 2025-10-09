using System.Threading.Tasks;

namespace Finance.Tracking.Services;

public class TransactionService : ITransactionService
{
    IAccountService _accountService;

    public TransactionService(IAccountService accountService)
    {
        _accountService = accountService;
    }
    public async Task ExecuteOrder(Order order)
    {
        Account? account = await _accountService.GetAccountByDateAsync(order.Account, order.CreatedAt);
        if (account is null)
            throw new NullReferenceException($"Account not found: {order.Account}");

        double cashChange = order.Quantity * order.Price;

        // Use dictionary safely
        var holdingsDict = account.HoldingsDict;

        // Ensure CASH holding exists
        if (!holdingsDict.TryGetValue(Symbol.CASH, out var cashHolding))
        {
            cashHolding = new Holding { Symbol = Symbol.CASH, Quantity = 0, AccountName = account.Name };
            account.Holdings.Add(cashHolding);
        }

        // Ensure the target asset holding exists
        if (!holdingsDict.TryGetValue(order.Symbol, out var assetHolding))
        {
            assetHolding = new Holding { Symbol = order.Symbol, Quantity = 0, AccountName = account.Name };
            account.Holdings.Add(assetHolding);
        }

        switch (order.Side)
        {
            case OrderSide.Sell:
                if (assetHolding.Quantity < order.Quantity)
                    throw new InvalidOperationException($"Not enough {order.Symbol} to sell.");
                assetHolding.Quantity -= order.Quantity;
                cashHolding.Quantity += cashChange; // Add cash from sale
                break;

            case OrderSide.Buy:
                if (cashHolding.Quantity < cashChange)
                    throw new InvalidOperationException($"Not enough CASH to buy {order.Symbol}.");
                assetHolding.Quantity += order.Quantity;
                cashHolding.Quantity -= cashChange; // Subtract cash for purchase
                break;
        }

        await _accountService.UpdateAccountAsync(account);
    }

}