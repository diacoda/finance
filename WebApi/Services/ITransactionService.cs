namespace Finance.Tracking.Services;

public interface ITransactionService
{
    public Task ExecuteOrder(Order order);
}