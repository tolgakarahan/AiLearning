using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;
using AiLearning.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAiServices(builder.Configuration);

using var host = builder.Build();

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