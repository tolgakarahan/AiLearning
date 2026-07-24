using AiLearning.Core.Features.SupportRequests.Models;

internal static class SupportRequestEvalCases
{
    public static IReadOnlyList<SupportRequestEvalCase> All { get; } =
    [
        new SupportRequestEvalCase
        {
            Name = "Defective product with explicit replacement request",
            Message =
                "Merhaba, 12345 numaralı siparişimdeki siyah kulaklığın sağ tarafından ses gelmiyor. Ürünün değiştirilmesini istiyorum.",
            Expected = new SupportRequestExtraction
            {
                OrderNumber = "12345",
                CustomerName = null,
                Email = null,
                IssueType = SupportIssueType.DefectiveProduct,
                RequestedAction = RequestedAction.Replacement,
                Urgency = SupportUrgency.Normal,
                AffectedProducts =
                [
                    new AffectedProduct
                    {
                        Name = "siyah kulaklık",
                        Problem = "Sağ tarafından ses gelmiyor."
                    }
                ],
                Summary =
                    "12345 numaralı siparişteki siyah kulaklığın sağ tarafından ses gelmiyor ve müşteri ürünün değiştirilmesini istiyor."
            }
        },
        new SupportRequestEvalCase
        {
            Name = "Missing product without urgency statement",
            Message =
                "78910 numaralı siparişim bugün ulaştı ancak kutudan şarj adaptörü çıkmadı. Eksik ürünün gönderilmesini rica ediyorum.",
            Expected = new SupportRequestExtraction
            {
                OrderNumber = "78910",
                CustomerName = null,
                Email = null,
                IssueType = SupportIssueType.MissingProduct,
                RequestedAction = RequestedAction.Replacement,
                Urgency = SupportUrgency.Normal,
                AffectedProducts =
                [
                    new AffectedProduct
                    {
                        Name = "şarj adaptörü",
                        Problem = "Sipariş paketinden çıkmadı."
                    }
                ],
                Summary =
                    "78910 numaralı siparişte şarj adaptörü eksik ve müşteri eksik ürünün gönderilmesini istiyor."
            }
        }
    ];
}