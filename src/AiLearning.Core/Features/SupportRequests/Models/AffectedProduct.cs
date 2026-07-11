namespace AiLearning.Core.Features.SupportRequests.Models;

public sealed class AffectedProduct
{
    public required string Name { get; init; }

    public required string Problem { get; init; }
}