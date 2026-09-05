using System.ComponentModel;
using IncidentAssistant.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace IncidentAssistant;

// This becomes an AIFunction/tool the agent can call. The [Description]
// attributes are what the model actually reads to decide *when* to call it -
// worth being deliberate with the wording.
public sealed class IncidentSearchTool(
    VectorStoreCollection<string, IncidentRecord> collection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    [Description("Searches production incident history for incidents relevant " +
                 "to the given query. Use this whenever the user asks about " +
                 "incidents, outages, root causes, or service issues.")]
    public async Task<string> SearchIncidentsAsync(
        [Description("A natural-language description of what to search for, " +
                     "e.g. 'error spikes in OrderService' or 'database deadlocks'.")]
        string query,
        [Description("Max number of incidents to return.")] int topK = 3)
    {
        var queryEmbedding = await embeddingGenerator.GenerateAsync([query]);

        var results = collection.SearchAsync(queryEmbedding[0].Vector, topK);

        var matches = new List<string>();
        await foreach (var result in results)
        {
            var r = result.Record;
            matches.Add(
                $"[{r.IncidentId}] ({r.Severity}, {r.Service}, {r.Timestamp}) " +
                $"{r.Summary} Root cause: {r.RootCause} Resolution: {r.Resolution}");
        }

        return matches.Count > 0
            ? string.Join("\n", matches)
            : "No relevant incidents found.";
    }
}
