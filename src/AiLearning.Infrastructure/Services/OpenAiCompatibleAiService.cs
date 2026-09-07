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
    private readonly IOrderService _orderService;

    private static readonly JsonSerializerOptions ToolJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        AiOptions options, IOrderService orderService)
    {
        _client = responsesClient;
        _chatClient = chatClient;
        _options = options;
        _orderService = orderService;
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

    public async Task<AiResponse> AskWithToolsAsync(
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

        var options = new ChatCompletionOptions();

        options.Tools.Add(GetOrderStatusTool);
        options.Tools.Add(CancelOrderTool);

        const int maxIterations = 5;

        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            var completion = await _chatClient.CompleteChatAsync(
                chatMessages,
                options,
                cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"Finish Reason : {completion.Value.FinishReason}");
            Console.WriteLine($"Content Count : {completion.Value.Content.Count}");
            Console.WriteLine($"Tool Count    : {completion.Value.ToolCalls.Count}");

            if (completion.Value.FinishReason == ChatFinishReason.Stop)
            {
                var text = string.Concat(
                    completion.Value.Content.Select(x => x.Text));

                return new AiResponse
                {
                    Text = text,
                    Model = _options.Model,
                    Provider = _options.Provider
                };
            }

            if (completion.Value.FinishReason != ChatFinishReason.ToolCalls)
            {
                throw new InvalidOperationException(
                    $"Unexpected finish reason: {completion.Value.FinishReason}");
            }

            chatMessages.Add(new AssistantChatMessage(completion.Value));

            foreach (var toolCall in completion.Value.ToolCalls)
            {
                Console.WriteLine();
                Console.WriteLine($"Tool Call Id : {toolCall.Id}");
                Console.WriteLine($"Tool Name    : {toolCall.FunctionName}");
                Console.WriteLine($"Arguments    : {toolCall.FunctionArguments}");

                var toolResult = toolCall.FunctionName switch
                {
                    "get_order_status" => await ExecuteGetOrderStatusAsync(
                        toolCall.FunctionArguments,
                        cancellationToken),

                    "cancel_order" => await ExecuteCancelOrderAsync(
                        toolCall.FunctionArguments,
                        cancellationToken),

                    _ => JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = $"Unsupported tool: {toolCall.FunctionName}"
                    })
                };

                Console.WriteLine($"Tool Result  : {toolResult}");

                chatMessages.Add(
                    new ToolChatMessage(
                        toolCall.Id,
                        toolResult));
            }
        }

        throw new InvalidOperationException(
            $"Tool execution exceeded the maximum iteration count of {maxIterations}.");
    }

    private async Task<string> ExecuteGetOrderStatusAsync(
        BinaryData functionArguments,
        CancellationToken cancellationToken)
    {
        GetOrderStatusArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<GetOrderStatusArguments>(
                functionArguments.ToString(),
                ToolJsonOptions);
        }
        catch (JsonException exception)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Tool arguments contain invalid JSON.",
                detail = exception.Message
            });
        }

        if (string.IsNullOrWhiteSpace(arguments?.OrderNumber))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "The order number is required."
            });
        }

        var status = await _orderService.GetStatusAsync(
            arguments.OrderNumber,
            cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = true,
            orderNumber = arguments.OrderNumber,
            status
        });
    }

    private async Task<string> ExecuteCancelOrderAsync(
        BinaryData functionArguments,
        CancellationToken cancellationToken)
    {
        CancelOrderArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<CancelOrderArguments>(
                functionArguments.ToString(),
                ToolJsonOptions);
        }
        catch (JsonException exception)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Tool arguments contain invalid JSON.",
                detail = exception.Message
            });
        }

        if (string.IsNullOrWhiteSpace(arguments?.OrderNumber))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "The order number is required."
            });
        }

        var result = await _orderService.CancelAsync(
            arguments.OrderNumber,
            cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = result.Status == CancelOrderStatus.Cancelled,
            orderNumber = arguments.OrderNumber,
            status = result.Status.ToString()
        });
    }

    private static readonly ChatTool GetOrderStatusTool =
    ChatTool.CreateFunctionTool(
        functionName: "get_order_status",
        functionDescription:
            "Gets the current processing, shipping, or delivery status of an order by its order number.",
        functionParameters: BinaryData.FromString(
            """
            {
              "type": "object",
              "properties": {
                "orderNumber": {
                  "type": "string",
                  "description": "The order number to query."
                }
              },
              "required": [
                "orderNumber"
              ],
              "additionalProperties": false
            }
            """));


    private static readonly ChatTool CancelOrderTool =
        ChatTool.CreateFunctionTool(
            functionName: "cancel_order",
            functionDescription:
                "Cancels an order when the user explicitly requests cancellation.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "orderNumber": {
                      "type": "string",
                      "description": "The order number to cancel."
                    }
                  },
                  "required": [
                    "orderNumber"
                  ],
                  "additionalProperties": false
                }
                """));

}