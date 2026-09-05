using System.Text.Json;
using IncidentAssistant.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace IncidentAssistant;

// Reads the sample incident JSON, embeds each one, and upserts it into the
// Qdrant "incidents" collection. Re-run-safe: each incident's Id is
// deterministic (its IncidentId), so re-running just overwrites the same points.
public static class Ingestion
{
    private sealed record RawIncident(
        string Id, string Service, string Timestamp, string Severity,
        string Summary, string RootCause, string Resolution);

    public static async Task RunAsync(
        VectorStoreCollection<string, IncidentRecord> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        await collection.EnsureCollectionExistsAsync();

        var json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "SampleData", "incidents.json"));
        var raw = JsonSerializer.Deserialize<List<RawIncident>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Could not parse incidents.json");

        foreach (var incident in raw)
        {
            var embeddingText =
                $"Service: {incident.Service}. Severity: {incident.Severity}. " +
                $"Summary: {incident.Summary}. Root cause: {incident.RootCause}. " +
                $"Resolution: {incident.Resolution}.";

            var embedding = await embeddingGenerator.GenerateAsync([embeddingText]);

            var record = new IncidentRecord
            {
                Id = incident.Id,
                IncidentId = incident.Id,
                Service = incident.Service,
                Severity = incident.Severity,
                Summary = incident.Summary,
                RootCause = incident.RootCause,
                Resolution = incident.Resolution,
                Timestamp = incident.Timestamp,
                ContentEmbedding = embedding[0].Vector
            };

            await collection.UpsertAsync(record);
        }

        Console.WriteLine($"Ingested {raw.Count} incidents into Qdrant.");
    }
}
