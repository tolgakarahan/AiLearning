using System.Text.Json;
using AiLearning.Console.Features.SupportRequestExtraction;
using AiLearning.Core.Features.SupportRequests.Models;
using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;

internal sealed class SupportRequestEvalRunner
{
    private readonly IAiService _aiService;

    public SupportRequestEvalRunner(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<bool> RunAsync(SupportRequestEvalCase evalCase, CancellationToken cancellationToken = default)
    {
        var messages = new List<AiMessage>
        {
            new()
            {
                Role = "system",
                Content = SupportRequestExtractionPrompt.System
            },

            new()
            {
                Role = "user",
                Content = evalCase.Message
            }
        };

        var actual = await _aiService.AskStructuredAsync<SupportRequestExtraction>(messages, cancellationToken);

        var summaryComparison = await CompareSummaryAsync(
            evalCase.Expected.Summary,
            actual.Summary,
            cancellationToken);

        var affectedProductsComparison = await CompareAffectedProductsAsync(
            evalCase.Expected.AffectedProducts,
            actual.AffectedProducts,
            cancellationToken);


        var comparison = SupportRequestComparer.Compare(evalCase.Expected, actual);

        Console.WriteLine();
        Console.WriteLine("COMPARISON");
        Console.WriteLine($"Order Number     : {comparison.OrderNumberMatches}");
        Console.WriteLine($"Customer Name    : {comparison.CustomerNameMatches}");
        Console.WriteLine($"Email            : {comparison.EmailMatches}");
        Console.WriteLine($"Issue Type       : {comparison.IssueTypeMatches}");
        Console.WriteLine($"Requested Action : {comparison.RequestedActionMatches}");
        Console.WriteLine($"Urgency          : {comparison.UrgencyMatches}");

        Console.WriteLine();
        Console.WriteLine("SEMANTIC COMPARISON");
        Console.WriteLine($"Summary          : {(summaryComparison.IsEquivalent ? "PASS" : "FAIL")}");
        Console.WriteLine($"Reason           : {summaryComparison.Reason}");

        var failedFields = comparison.GetFailedExactFields();

        if (failedFields.Count > 0)
        {
            Console.WriteLine($"Failed Fields    : {string.Join(", ", failedFields)}");
        }

        Console.WriteLine();
        Console.WriteLine("AFFECTED PRODUCTS COMPARISON");
        Console.WriteLine(
            $"Affected Products : " +
            $"{(affectedProductsComparison.IsEquivalent ? "PASS" : "FAIL")}");
        Console.WriteLine(
            $"Reason            : {affectedProductsComparison.Reason}");


        Console.WriteLine($"Case: {evalCase.Name}");
        Console.WriteLine();

        Console.WriteLine("EXPECTED");
        Print(evalCase.Expected);

        Console.WriteLine();

        Console.WriteLine("ACTUAL");
        Print(actual);

        var overallPassed = 
            comparison.ExactFieldsMatch &&
            summaryComparison.IsEquivalent &&
            affectedProductsComparison.IsEquivalent;

        Console.WriteLine();
        Console.WriteLine("OVERALL RESULT");
        Console.WriteLine(overallPassed ? "PASS" : "FAIL");

        return overallPassed;
    }

    private async Task<SemanticComparisonResult> CompareSummaryAsync(
    string expected,
    string actual,
    CancellationToken cancellationToken)
    {
        var messages = new List<AiMessage>
    {
        new()
        {
            Role = "system",
            Content =
                """
                You are evaluating the semantic equivalence of two customer support summaries.

                Decide whether both summaries communicate the same essential facts.

                Ignore:
                - wording differences
                - sentence structure
                - capitalization
                - punctuation
                - minor grammar or spelling mistakes

                Return IsEquivalent as false if:
                - an important fact is missing
                - a new unsupported fact is added
                - the requested action is different
                - the product, problem, order number, or urgency meaning changes
                - the summaries contradict each other

                The texts may be written in Turkish.
                """
        },
        new()
        {
            Role = "user",
            Content =
                $"""
                Expected summary:
                {expected}

                Actual summary:
                {actual}
                """
        }
    };

        return await _aiService.AskStructuredAsync<SemanticComparisonResult>(
            messages,
            cancellationToken);
    }

    private async Task<AffectedProductsComparisonResult>
    CompareAffectedProductsAsync(
        IReadOnlyList<AffectedProduct> expected,
        IReadOnlyList<AffectedProduct> actual,
        CancellationToken cancellationToken)
    {
        var expectedJson = JsonSerializer.Serialize(expected);
        var actualJson = JsonSerializer.Serialize(actual);

        var messages = new List<AiMessage>
        {
            new()
            {
                Role = "system",
                Content =
                    """
                    You are evaluating two extracted customer support product lists.

                    Decide whether the expected and actual lists contain the same
                    affected products and communicate equivalent problems for each product.

                    Ignore:
                    - list ordering
                    - capitalization
                    - punctuation
                    - minor grammar or spelling differences
                    - wording differences that preserve the same meaning
                    - equivalent product names such as abbreviations or common synonyms

                    Return IsEquivalent as false if:
                    - an expected product is missing
                    - an unsupported product is added
                    - a product is matched with the wrong problem
                    - an important part of a product problem is missing
                    - the problem meanings contradict each other

                    Base your decision only on the two lists provided.

                    In the Reason field, clearly explain any meaningful difference.
                    If there is no meaningful difference, state that the lists are equivalent.

                    Product names and problem descriptions may be written in Turkish.
                    """
            },
            new()
            {
                Role = "user",
                Content =
                    $"""
                    Expected affected products:
                    {expectedJson}

                    Actual affected products:
                    {actualJson}
                    """
            }
        };

        return await _aiService
            .AskStructuredAsync<AffectedProductsComparisonResult>(
                messages,
                cancellationToken);
    }

    private static void Print(SupportRequestExtraction result)
    {
        Console.WriteLine($"Order Number     : {result.OrderNumber}");
        Console.WriteLine($"Customer Name    : {result.CustomerName}");
        Console.WriteLine($"Email            : {result.Email}");
        Console.WriteLine($"Issue Type       : {result.IssueType}");
        Console.WriteLine($"Requested Action : {result.RequestedAction}");
        Console.WriteLine($"Urgency          : {result.Urgency}");
        Console.WriteLine($"Summary          : {result.Summary}");

        Console.WriteLine("Affected Products:");

        foreach (var product in result.AffectedProducts)
        {
            Console.WriteLine(
                $"- {product.Name}: {product.Problem}");
        }
    }
}