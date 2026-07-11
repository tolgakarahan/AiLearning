namespace AiLearning.Core.Models;

public sealed class AiResponse
{
    public required string Text { get; init; }
    public required string Model { get; init; }
    public required string Provider { get; init; }
}