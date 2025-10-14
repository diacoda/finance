namespace Finance.Tracking.Interfaces;

public interface ITransactionService
{
    public Task ExecuteOrder(Order order);
}