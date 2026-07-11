namespace AiLearning.Core.Models;

public sealed class UserIntentResult
{
    public required UserIntent Intent { get; init; }
    public required string Language { get; init; }
    public required double Confidence { get; init; }
    public required string Summary { get; init; }
}