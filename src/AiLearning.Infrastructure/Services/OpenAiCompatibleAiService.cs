#pragma warning disable OPENAI001

using System.Text.Json;
using AiLearning.Core.Interfaces;
using AiLearning.Core.Models;
using OpenAI.Responses;
using OpenAI.Chat;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AiLearning.Infrastructure.Services;

public sealed class OpenAiCompatibleAiService : IAiService
{
    private readonly ResponsesClient _client;
    private readonly ChatClient _chatClient;
    private readonly AiOptions _options;

    private static readonly JsonSerializerOptions StructuredJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private static readonly JsonSchemaExporterOptions SchemaExporterOptions =
        new()
        {
            TreatNullObliviousAsNonNullable = true,

            TransformSchemaNode = (_, schema) =>
            {
                if (schema is not JsonObject schemaObject)
                {
                    return schema;
                }

                if (schemaObject["properties"] is not JsonObject properties)
                {
                    return schema;
                }

                schemaObject["additionalProperties"] = false;

                var requiredProperties = new JsonArray();

                foreach (var property in properties)
                {
                    requiredProperties.Add(property.Key);
                }

                schemaObject["required"] = requiredProperties;

                return schemaObject;
            }
        };

    private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                RespectNullableAnnotations = true,
                RespectRequiredConstructorParameters = true
            };

    public OpenAiCompatibleAiService(
        ResponsesClient responsesClient,
        ChatClient chatClient,
        AiOptions options)
    {
        _client = responsesClient;
        _chatClient = chatClient;
        _options = options;
    }

    public async Task<AiResponse> AskAsync(
    IReadOnlyList<AiMessage> messages,
    CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            throw new ArgumentException("En az bir mesaj gönderilmelidir.", nameof(messages));

        var request = new CreateResponseOptions
        {
            Model = _options.Model
        };

        if (!string.IsNullOrWhiteSpace(_options.SystemPrompt))
        {
            request.Instructions = _options.SystemPrompt;
        }

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                continue;

            if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                request.InputItems.Add(
                    ResponseItem.CreateUserMessageItem(message.Content));
            }
            else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                request.InputItems.Add(
                    ResponseItem.CreateAssistantMessageItem(message.Content));
            }
        }

        var response = await _client.CreateResponseAsync(
            request,
            cancellationToken);

        return new AiResponse
        {
            Text = response.Value.GetOutputText(),
            Model = _options.Model,
            Provider = _options.Provider
        };
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
     IReadOnlyList<AiMessage> messages,
     [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
            throw new ArgumentException("En az bir mesaj gönderilmelidir.", nameof(messages));

        var request = new CreateResponseOptions
        {
            Model = _options.Model,
            StreamingEnabled = true
        };

        if (!string.IsNullOrWhiteSpace(_options.SystemPrompt))
        {
            request.Instructions = _options.SystemPrompt;
        }

        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                continue;

            if (message.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                request.InputItems.Add(
                    ResponseItem.CreateUserMessageItem(message.Content));
            }
            else if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                request.InputItems.Add(
                    ResponseItem.CreateAssistantMessageItem(message.Content));
            }
        }

        var stream = _client.CreateResponseStreamingAsync(
            request,
            cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            //Console.WriteLine(update.GetType().Name);
            if (update is StreamingResponseOutputTextDeltaUpdate deltaUpdate)
            {
                if (!string.IsNullOrEmpty(deltaUpdate.Delta))
                {
                    yield return deltaUpdate.Delta;
                }
            }
        }
    }

    public async Task<T> AskStructuredAsync<T>(
     IReadOnlyList<AiMessage> messages,
     CancellationToken cancellationToken = default)
    {
        var chatMessages = messages
            .Select<AiMessage, ChatMessage>(message =>
                message.Role.ToLowerInvariant() switch
                {
                    "system" => new SystemChatMessage(message.Content),
                    "assistant" => new AssistantChatMessage(message.Content),
                    _ => new UserChatMessage(message.Content)
                })
            .ToList();

        JsonNode schemaNode = StructuredJsonOptions.GetJsonSchemaAsNode(typeof(T), SchemaExporterOptions);
 
        BinaryData jsonSchema = BinaryData.FromString(schemaNode.ToJsonString());

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: typeof(T).Name,
                jsonSchema: jsonSchema,
                jsonSchemaIsStrict: true)
        };

        var completion = await _chatClient.CompleteChatAsync(
            chatMessages,
            options,
            cancellationToken);

        var content = completion.Value.Content;

        if (content.Count == 0)
        {
            throw new InvalidOperationException(
                "AI structured response boş content döndürdü.");
        }

        var json = content[0].Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "AI structured response boş JSON döndürdü.");
        }

        var result = JsonSerializer.Deserialize<T>(
            json,
            StructuredJsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "AI cevabı boş JSON sonucu üretti.");
        }

        return result;
    }

    private static string CleanJsonResponse(string text)
    {
        var cleaned = text.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["```json".Length..].Trim();
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned["```".Length..].Trim();
        }

        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned[..^3].Trim();
        }

        return cleaned;
    }



}