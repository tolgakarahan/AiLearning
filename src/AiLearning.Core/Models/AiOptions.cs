namespace AiLearning.Core.Models;

public sealed class AiOptions
{
    public string Provider { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKeyEnvironmentVariableName { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string SystemPrompt { get; init; } = string.Empty;
}