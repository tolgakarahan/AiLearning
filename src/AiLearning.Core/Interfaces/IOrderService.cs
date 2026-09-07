using AiLearning.Core.Models;

public interface IOrderService
{
    Task<string> GetStatusAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);

    Task<CancelOrderResult> CancelAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);
}
