using IncidentAssistant;
using IncidentAssistant.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using Qdrant.Client;

// --- Step 2: RAG over Qdrant, agentic tool-calling via Microsoft.Extensions.AI ---
//
// Flow:
//   1. Connect to OpenAI (chat + embeddings) and local Qdrant.
//   2. Ingest incidents.json into Qdrant as embeddings (idempotent - safe to
//      re-run; it just re-upserts the same points).
//   3. Wrap the chat client with .UseFunctionInvocation() and give it the
//      incident-search tool. The model decides on its own, per question,
//      whether it needs to call the tool before answering - this is the
//      "agentic" behavior, without depending on the newer (still pre-GA)
//      Microsoft Agent Framework package.

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Set OPENAI_API_KEY to your OpenAI API key.");
var chatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";
var embeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";
var qdrantHost = Environment.GetEnvironmentVariable("QDRANT_HOST") ?? "localhost";

OpenAIClient openAiClient = new(apiKey);

// UseFunctionInvocation() wraps the base chat client so that when the model
// requests a tool call, Microsoft.Extensions.AI automatically invokes the
// matching C# method and feeds the result back - no manual tool-call loop needed.
IChatClient chatClient = openAiClient
    .GetChatClient(chatModel)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    openAiClient.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator();

// Qdrant vector store - assumes `docker compose up` is running (see docker-compose.yml)
var qdrantClient = new QdrantClient(qdrantHost, 6334);
var vectorStore = new QdrantVectorStore(qdrantClient, ownsClient: true);
var collection = vectorStore.GetCollection<string, IncidentRecord>("incidents");

// --- Ingest sample data (safe to re-run) ---
await Ingestion.RunAsync(collection, embeddingGenerator);

// --- Build the RAG tool ---
var searchTool = new IncidentSearchTool(collection, embeddingGenerator);
AIFunction searchFunction = AIFunctionFactory.Create(searchTool.SearchIncidentsAsync);

var chatOptions = new ChatOptions { Tools = [searchFunction] };

List<ChatMessage> conversation =
[
    new(ChatRole.System, """
        You are an Ops Assistant that helps engineers understand and triage
        production incidents. Use the incident search tool to find relevant
        incidents before answering - don't guess. Cite incident IDs (e.g.
        INC-1001) in your answers. If the tool finds nothing relevant, say so.
        """)
];

Console.WriteLine("Incident Assistant (v2 - RAG over Qdrant) - ask a question, or 'exit' to quit.");
Console.WriteLine("Example: \"What caused the error spike in OrderService?\"");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    conversation.Add(new ChatMessage(ChatRole.User, question));

    var response = await chatClient.GetResponseAsync(conversation, chatOptions);
    Console.WriteLine();
    Console.WriteLine(response.Text);
    Console.WriteLine();

    conversation.Add(new ChatMessage(ChatRole.Assistant, response.Text));
}
