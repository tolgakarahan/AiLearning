using AiLearning.Console.Features.SupportRequestExtraction;
using AiLearning.Core.Features.SupportRequests.Models;
using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;
using AiLearning.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAiServices(builder.Configuration);

using var host = builder.Build();

using var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

var aiService = host.Services.GetRequiredService<IAiService>();


var messagesForToolCall = new List<AiMessage>
{
    new()
    {
        Role = "user",
        Content = "78910 numaralı siparişim gecikti. Böyle devam ederse iptal edeceğim."
    }
};

var response = await aiService.AskWithToolsAsync(messagesForToolCall, cancellationToken);

Console.WriteLine(response.Text);
return;




var evalRunner = new SupportRequestEvalRunner(aiService);

var passedCount = 0;
var failedCount = 0;
var errorCount = 0;

foreach (var evalCase in SupportRequestEvalCases.All)
{
    if (cancellationToken.IsCancellationRequested)
    {
        Console.WriteLine("Evaluation cancelled.");
        break;
    }

    try
    {
        var passed = await evalRunner.RunAsync(evalCase, cancellationToken);

        if (passed)
            passedCount++;
        else
            failedCount++;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        Console.WriteLine();
        Console.WriteLine("Evaluation cancelled.");
        break;
    }
    catch (Exception exception)
    {
        errorCount++;


        Console.WriteLine();
        Console.WriteLine($"EVAL ERROR: {exception.Message}");
    }


    Console.WriteLine();
    Console.WriteLine(new string('=', 60));
    Console.WriteLine();
}

var evaluatedCount = passedCount + failedCount;

var passRate = evaluatedCount == 0
    ? 0
    : (double)passedCount / evaluatedCount * 100;

var totalCount = SupportRequestEvalCases.All.Count;

Console.WriteLine("EVALUATION SUMMARY");
Console.WriteLine($"Total  : {totalCount}");
Console.WriteLine($"Passed : {passedCount}");
Console.WriteLine($"Failed : {failedCount}");
Console.WriteLine($"Errors : {errorCount}");
Console.WriteLine($"Pass Rate : {passRate:F1}%");

return;

var messages = new List<AiMessage>
{
    new AiMessage
    {
        Role = "system",
        Content = SupportRequestExtractionPrompt.System
    },

    new AiMessage
    {
        Role = "user",
        Content = """
        Dün aldığım siyah kulaklığın sağ tarafından ses gelmiyor.
        """
    }
};

var result =
    await aiService.AskStructuredAsync<SupportRequestExtraction>(messagesForToolCall);

Console.WriteLine($"Order Number     : {result.OrderNumber ?? "null"}");
Console.WriteLine($"Customer Name    : {result.CustomerName ?? "null"}");
Console.WriteLine($"Email            : {result.Email ?? "null"}");
Console.WriteLine($"Issue Type       : {result.IssueType}");
Console.WriteLine($"Requested Action : {result.RequestedAction}");
Console.WriteLine($"Urgency          : {result.Urgency}");
Console.WriteLine($"Summary          : {result.Summary}");

Console.WriteLine("Affected Products:");

foreach (var product in result.AffectedProducts)
{
    Console.WriteLine($"- {product.Name}: {product.Problem}");
}



return;





Console.WriteLine("AI Learning");
Console.WriteLine("1 - Chat");
Console.WriteLine("2 - Structured Outputs Demo");
Console.Write("Seçim: ");

var choice = Console.ReadLine();

if (choice == "2")
{
    await RunStructuredOutputsDemoAsync(host.Services);
    return;
}

await RunChatAsync(host.Services);

static async Task RunChatAsync(IServiceProvider services)
{
    var chatSession = services.GetRequiredService<IAiChatSession>();

    Console.WriteLine();
    Console.WriteLine("AI Chat başladı. Çıkmak için 'exit' yaz.");
    Console.WriteLine("Cevabı iptal etmek için Ctrl+C kullan.");
    Console.WriteLine();

    while (true)
    {
        Console.Write("Sen: ");
        var question = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(question))
            continue;

        if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        using var cts = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            Console.WriteLine();
            Console.Write("AI: ");

            await foreach (var token in chatSession.AskStreamingAsync(question, cts.Token))
            {
                Console.Write(token);
            }

            Console.WriteLine();
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("[Cevap iptal edildi]");
            Console.WriteLine();
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}

static async Task RunStructuredOutputsDemoAsync(IServiceProvider services)
{
    var analyzer = services.GetRequiredService<IUserIntentAnalyzer>();

    Console.WriteLine();
    Console.WriteLine("Structured Outputs Demo");
    Console.WriteLine();

    Console.Write("Analiz edilecek mesaj: ");
    var message = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(message))
    {
        Console.WriteLine("Mesaj boş olamaz.");
        return;
    }

    var result = await analyzer.AnalyzeAsync(message);

    Console.WriteLine();
    Console.WriteLine($"Intent     : {result.Intent}");
    Console.WriteLine($"Language   : {result.Language}");
    Console.WriteLine($"Confidence : {result.Confidence}");
    Console.WriteLine($"Summary    : {result.Summary}");
    Console.WriteLine();

    Console.WriteLine();

    switch (result.Intent)
    {
        case UserIntent.Question:
            Console.WriteLine("Karar      : Bu mesaj bir soru olarak ele alınabilir.");
            break;

        case UserIntent.Command:
            Console.WriteLine("Karar      : Bu mesaj bir komut olarak ele alınabilir.");
            break;

        case UserIntent.LearningRequest:
            Console.WriteLine("Karar      : Bu mesaj öğrenme isteği olarak ele alınabilir.");
            break;

        case UserIntent.CodingHelp:
            Console.WriteLine("Karar      : Bu mesaj kod yardımı olarak ele alınabilir.");
            break;

        case UserIntent.SmallTalk:
            Console.WriteLine("Karar      : Bu mesaj sohbet olarak ele alınabilir.");
            break;

        default:
            Console.WriteLine("Karar      : Intent net değil.");
            break;
    }
}