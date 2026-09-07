#pragma warning disable OPENAI001

using System.ClientModel;
using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;
using AiLearning.Core.Services;
using AiLearning.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace AiLearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiOptions = configuration
            .GetSection("AI")
            .Get<AiOptions>();

        if (aiOptions is null)
            throw new InvalidOperationException("AI configuration bulunamadı.");

        ValidateAiOptions(aiOptions);

        services.AddSingleton(aiOptions);   
        services.AddScoped<IUserIntentAnalyzer, UserIntentAnalyzer>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<AiOptions>();

            var apiKey = Environment.GetEnvironmentVariable(
                options.ApiKeyEnvironmentVariableName);

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    $"{options.ApiKeyEnvironmentVariableName} bulunamadı.");

            return new ResponsesClient(
                credential: new ApiKeyCredential(apiKey),
                options: new ResponsesClientOptions
                {
                    Endpoint = new Uri(options.Endpoint)
                });
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<AiOptions>();

            var apiKey = Environment.GetEnvironmentVariable(
                options.ApiKeyEnvironmentVariableName);

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    $"{options.ApiKeyEnvironmentVariableName} bulunamadı.");

            return new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.Endpoint)
                });
        });

        services.AddSingleton<IAiService, OpenAiCompatibleAiService>();
        services.AddSingleton<IAiChatSession, AiChatSession>();
        services.AddSingleton<IOrderService, FakeOrderService>();

        return services;
    }

    private static void ValidateAiOptions(AiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
            throw new InvalidOperationException("AI:Provider boş olamaz.");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException("AI:Endpoint boş olamaz.");

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException("AI:Endpoint geçerli bir URL olmalı.");

        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("AI:Model boş olamaz.");

        if (string.IsNullOrWhiteSpace(options.ApiKeyEnvironmentVariableName))
            throw new InvalidOperationException("AI:ApiKeyEnvironmentVariableName boş olamaz.");

        var provider = options.Provider.Trim().ToLowerInvariant();

        var supportedProviders = new[]
        {
        "openrouter",
        "lmstudio",
        "openai"
    };

        if (!supportedProviders.Contains(provider))
            throw new NotSupportedException($"'{options.Provider}' provider'ı desteklenmiyor.");
    }
}