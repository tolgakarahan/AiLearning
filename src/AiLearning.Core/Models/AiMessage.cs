namespace AiLearning.Core.Models;

public sealed class AiMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}