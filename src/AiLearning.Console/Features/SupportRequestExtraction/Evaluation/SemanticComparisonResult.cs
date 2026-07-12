internal sealed class SemanticComparisonResult
{
    public required bool IsEquivalent { get; init; }

    public required string Reason { get; init; }
}