using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;

namespace AiLearning.Infrastructure.Services;

public sealed class UserIntentAnalyzer : IUserIntentAnalyzer
{
    private readonly IAiService _aiService;

    public UserIntentAnalyzer(IAiService aiService)
    {
        _aiService = aiService;
    }

    public Task<UserIntentResult> AnalyzeAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        return _aiService.AskStructuredAsync<UserIntentResult>(
        [
            new AiMessage
            {
                Role = "user",
                Content = $$"""
                    Aşağıdaki kullanıcı mesajını analiz et.

                    Intent seçenekleri:
                    - Question: Kullanıcı bir soru soruyorsa.
                    - Command: Kullanıcı bir işlem yapılmasını istiyorsa.
                    - LearningRequest: Kullanıcı bir konuyu öğrenmek istiyorsa.
                    - CodingHelp: Kullanıcı kodlama veya yazılım geliştirme yardımı istiyorsa.
                    - SmallTalk: Kullanıcı gündelik sohbet ediyorsa.
                    - Unknown: Intent net değilse.

                    Kurallar:
                    - Kullanıcının niyetini en uygun intent ile sınıflandır.
                    - Confidence 0 ile 1 arasında olmalı.
                    - Summary kısa ve Türkçe olmalı.
                    - Emin değilsen Unknown kullan.

                    Kullanıcı mesajı:
                    "{{userMessage}}"
                    """
            }
        ], cancellationToken);
    }
}