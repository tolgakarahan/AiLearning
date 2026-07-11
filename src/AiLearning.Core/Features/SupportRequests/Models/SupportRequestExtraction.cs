namespace AiLearning.Core.Features.SupportRequests.Models;

public sealed class SupportRequestExtraction
{
    public string? OrderNumber { get; init; }

    public string? CustomerName { get; init; }

    public string? Email { get; init; }

    public required SupportIssueType IssueType { get; init; }

    public required RequestedAction RequestedAction { get; init; }

    public required SupportUrgency Urgency { get; init; }

    public required List<AffectedProduct> AffectedProducts { get; init; }

    public required string Summary { get; init; }
}