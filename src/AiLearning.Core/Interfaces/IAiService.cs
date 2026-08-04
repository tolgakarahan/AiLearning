using AiLearning.Core.Models;

namespace AiLearning.Core.Interfaces;

public interface IAiService
{
    Task<AiResponse> AskAsync(IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> AskStreamingAsync(IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default);
    Task<T> AskStructuredAsync<T>(IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default);
    Task InspectToolCallAsync(IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default);
}