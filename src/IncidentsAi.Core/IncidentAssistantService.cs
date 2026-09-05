using IncidentsAi.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;

namespace IncidentsAi.Core;

public sealed class IncidentAssistantOptions
{
    public required string OpenAiApiKey { get; init; }
    public string ChatModel { get; init; } = "gpt-4o-mini";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public string QdrantHost { get; init; } = "localhost";
}

// The single place that wires together OpenAI + Qdrant + the search tool and
// exposes one operation - AskAsync. Both IncidentsAi.Api and
// IncidentsAi.Console depend on this instead of duplicating setup code.
public sealed class IncidentAssistantService : IAsyncDisposable
{
    private readonly IChatClient _chatClient;
    private readonly VectorStoreCollection<string, IncidentRecord> _collection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IncidentSearchTool _searchTool;
    private readonly QdrantClient _qdrantClient;

    private const string SystemPrompt = """
        You are an Ops Assistant that helps engineers understand and triage
        production incidents. Use the incident search tool to find relevant
        incidents before answering - don't guess. Cite incident IDs (e.g.
        INC-1001) in your answers. If the tool finds nothing relevant, say so.
        """;

    public IncidentAssistantService(IncidentAssistantOptions options)
    {
        var openAiClient = new OpenAIClient(options.OpenAiApiKey);

        _chatClient = openAiClient
            .GetChatClient(options.ChatModel)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        _embeddingGenerator = openAiClient
            .GetEmbeddingClient(options.EmbeddingModel)
            .AsIEmbeddingGenerator();

        _qdrantClient = new QdrantClient(options.QdrantHost, 6334);
        var vectorStore = new QdrantVectorStore(_qdrantClient, ownsClient: true);
        _collection = vectorStore.GetCollection<string, IncidentRecord>("incidents");

        _searchTool = new IncidentSearchTool(_collection, _embeddingGenerator);
    }

    /// <summary>Ingests the sample incident data into Qdrant. Safe to call repeatedly. Returns the count ingested.</summary>
    public Task<int> IngestSampleDataAsync() =>
        Ingestion.RunAsync(_collection, _embeddingGenerator);

    public async Task<string> AskAsync(string question, List<ChatMessage>? conversation = null)
    {
        conversation ??= [];
        if (conversation.Count == 0)
            conversation.Add(new ChatMessage(ChatRole.System, SystemPrompt));

        conversation.Add(new ChatMessage(ChatRole.User, question));

        var searchFunction = AIFunctionFactory.Create(_searchTool.SearchIncidentsAsync);
        var options = new ChatOptions { Tools = [searchFunction] };

        var response = await _chatClient.GetResponseAsync(conversation, options);
        conversation.Add(new ChatMessage(ChatRole.Assistant, response.Text));

        return response.Text;
    }

    public ValueTask DisposeAsync()
    {
        _qdrantClient.Dispose();
        return ValueTask.CompletedTask;
    }
}
