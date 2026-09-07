using AiLearning.Core.Models;

public sealed class FakeOrderService : IOrderService
{
    public Task<string> GetStatusAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        var status = orderNumber switch
        {
            "12345" => "Shipped",
            "78910" => "Processing",
            _ => "NotFound"
        };

        return Task.FromResult(status);
    }

    public Task<CancelOrderResult> CancelAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        var status = orderNumber switch
        {
            "78910" => CancelOrderStatus.Cancelled,
            "12345" => CancelOrderStatus.CannotCancel,
            _ => CancelOrderStatus.NotFound
        };

        return Task.FromResult(
            new CancelOrderResult
            {
                Status = status
            });
    }
}
