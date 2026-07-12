using AiLearning.Core.Features.SupportRequests.Models;

internal sealed class SupportRequestEvalCase
{
    public required string Name { get; init; }

    public required string Message { get; init; }

    public required SupportRequestExtraction Expected { get; init; }
}