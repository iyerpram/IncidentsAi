using IncidentsAi.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace IncidentsAi.Core;

// Embeds each sample incident and upserts it into the Qdrant "incidents"
// collection. Re-run-safe: each incident's Id is deterministic (its
// IncidentId), so re-running just overwrites the same points.
public static class Ingestion
{
    public static async Task<int> RunAsync(
        VectorStoreCollection<string, IncidentRecord> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        await collection.EnsureCollectionExistsAsync();

        var incidents = IncidentDataLoader.LoadSampleIncidents();

        foreach (var incident in incidents)
        {
            var embedding = await embeddingGenerator.GenerateAsync([incident.ToContentText()]);

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

        return incidents.Count;
    }
}
