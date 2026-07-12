using AiLearning.Core.Features.SupportRequests.Models;

internal sealed class SupportRequestComparison
{
    public bool OrderNumberMatches { get; init; }

    public bool CustomerNameMatches { get; init; }

    public bool EmailMatches { get; init; }

    public bool IssueTypeMatches { get; init; }

    public bool RequestedActionMatches { get; init; }

    public bool UrgencyMatches { get; init; }

    public bool ExactFieldsMatch =>
    OrderNumberMatches &&
    CustomerNameMatches &&
    EmailMatches &&
    IssueTypeMatches &&
    RequestedActionMatches &&
    UrgencyMatches;

    public IReadOnlyList<string> GetFailedExactFields()
    {
        var failedFields = new List<string>();

        if (!OrderNumberMatches)
            failedFields.Add(nameof(SupportRequestExtraction.OrderNumber));

        if (!CustomerNameMatches)
            failedFields.Add(nameof(SupportRequestExtraction.CustomerName));

        if (!EmailMatches)
            failedFields.Add(nameof(SupportRequestExtraction.Email));

        if (!IssueTypeMatches)
            failedFields.Add(nameof(SupportRequestExtraction.IssueType));

        if (!RequestedActionMatches)
            failedFields.Add(nameof(SupportRequestExtraction.RequestedAction));

        if (!UrgencyMatches)
            failedFields.Add(nameof(SupportRequestExtraction.Urgency));

        return failedFields;
    }
}