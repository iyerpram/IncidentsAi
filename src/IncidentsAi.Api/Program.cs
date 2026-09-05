using IncidentsAi.Core;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// CORS so the Blazor Web front end (running on a different port locally) can call this API.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()));

// Register IncidentAssistantService as a singleton - it holds long-lived
// clients (OpenAI, Qdrant) that are safe and efficient to reuse across requests.
builder.Services.AddSingleton(sp =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("Set OPENAI_API_KEY.");

    return new IncidentAssistantService(new IncidentAssistantOptions
    {
        OpenAiApiKey = apiKey,
        ChatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini",
        EmbeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small",
        QdrantHost = Environment.GetEnvironmentVariable("QDRANT_HOST") ?? "localhost"
    });
});

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/ingest", async (IncidentAssistantService assistant) =>
{
    var count = await assistant.IngestSampleDataAsync();
    return Results.Ok(new { ingested = count });
});

app.MapPost("/api/ask", async (AskRequest request, IncidentAssistantService assistant) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { error = "Question is required." });

    // NOTE: this creates a fresh conversation per request - fine for a demo/
    // portfolio project. A production version would persist conversation
    // history per session/user (e.g. keyed by a session ID) rather than
    // starting fresh every call.
    var answer = await assistant.AskAsync(request.Question);
    return Results.Ok(new AskResponse(answer));
});

app.Run();

public sealed record AskRequest(string Question);
public sealed record AskResponse(string Answer);
