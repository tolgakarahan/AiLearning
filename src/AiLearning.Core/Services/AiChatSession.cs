using System.Runtime.CompilerServices;
using System.Text;
using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;

namespace AiLearning.Core.Services;

public sealed class AiChatSession : IAiChatSession
{
    private readonly IAiService _aiService;
    private readonly List<AiMessage> _messages = [];

    public AiChatSession(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _messages.Add(new AiMessage
        {
            Role = "user",
            Content = userMessage
        });

        var assistantMessage = new StringBuilder();

        try
        {
            await foreach (var token in _aiService
                               .AskStreamingAsync(_messages, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                assistantMessage.Append(token);
                yield return token;
            }
        }
        finally
        {
            if (assistantMessage.Length > 0)
            {
                _messages.Add(new AiMessage
                {
                    Role = "assistant",
                    Content = assistantMessage.ToString()
                });
            }
        }
    }
}