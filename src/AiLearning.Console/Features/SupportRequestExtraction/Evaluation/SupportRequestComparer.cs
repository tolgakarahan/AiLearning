using AiLearning.Core.Features.SupportRequests.Models;

internal static class SupportRequestComparer
{
    public static SupportRequestComparison Compare(
        SupportRequestExtraction expected,
        SupportRequestExtraction actual)
    {
        return new SupportRequestComparison
        {
            OrderNumberMatches =
                expected.OrderNumber == actual.OrderNumber,

            CustomerNameMatches =
                expected.CustomerName == actual.CustomerName,

            EmailMatches =
                expected.Email == actual.Email,

            IssueTypeMatches =
                expected.IssueType == actual.IssueType,

            RequestedActionMatches =
                expected.RequestedAction == actual.RequestedAction,

            UrgencyMatches =
                expected.Urgency == actual.Urgency
        };
    }
}