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
        Account? account = await _accountService.GetAccountAsync(order.Account);
        if (account is null)
            throw new NullReferenceException($"Account not found: {order.Account}");

        double cash = order.Quantity * order.Price;

        // Use dictionary safely
        var holdingsDict = account.HoldingsDict;

        if (!holdingsDict.TryGetValue(order.Symbol, out var holding))
        {
            // Create a new holding if it doesn't exist
            holding = new Holding { Symbol = order.Symbol, Quantity = 0 };
            account.Holdings.Add(holding); // EF Core tracks it
        }

        switch (order.Side)
        {
            case OrderSide.Sell:
                if (holding.Quantity < order.Quantity)
                    throw new InvalidOperationException($"Not enough {order.Symbol} to sell.");
                holding.Quantity -= order.Quantity;
                account.Cash += cash;
                break;

            case OrderSide.Buy:
                holding.Quantity += order.Quantity;
                account.Cash -= cash;
                break;
        }

        await _accountService.UpdateAccountAsync(account);
    }
}