namespace AiLearning.Core.Interfaces;

public interface IAiChatSession
{
    IAsyncEnumerable<string> AskStreamingAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}