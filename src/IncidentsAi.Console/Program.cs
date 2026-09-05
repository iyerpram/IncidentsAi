using IncidentsAi.Core;
using Microsoft.Extensions.AI;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Set OPENAI_API_KEY to your OpenAI API key.");

await using var assistant = new IncidentAssistantService(new IncidentAssistantOptions
{
    OpenAiApiKey = apiKey,
    ChatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini",
    EmbeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small",
    QdrantHost = Environment.GetEnvironmentVariable("QDRANT_HOST") ?? "localhost"
});

var count = await assistant.IngestSampleDataAsync();
Console.WriteLine($"Ingested {count} incidents into Qdrant.");
Console.WriteLine();
Console.WriteLine("Incident Assistant (console) - ask a question, or 'exit' to quit.");
Console.WriteLine("Example: \"What caused the error spike in OrderService?\"");
Console.WriteLine();

List<ChatMessage> conversation = [];

while (true)
{
    Console.Write("> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var answer = await assistant.AskAsync(question, conversation);
    Console.WriteLine();
    Console.WriteLine(answer);
    Console.WriteLine();
}
