using AiLearning.Core.Models;

namespace AiLearning.Core.Interfaces;

public interface IUserIntentAnalyzer
{
    Task<UserIntentResult> AnalyzeAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}