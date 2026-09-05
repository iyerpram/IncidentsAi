using Microsoft.Extensions.VectorData;

namespace IncidentsAi.Core.Models;

// This shape maps directly to a point in the Qdrant "incidents" collection.
// The [VectorStoreKey]/[VectorStoreData]/[VectorStoreVector] attributes come
// from Microsoft.Extensions.VectorData.Abstractions and let the same class
// work against Qdrant, Azure AI Search, in-memory, etc. with no code changes -
// only the store implementation registered at startup changes.
public sealed class IncidentRecord
{
    [VectorStoreKey]
    public required string Id { get; set; }

    [VectorStoreData]
    public required string IncidentId { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string Service { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string Severity { get; set; }

    [VectorStoreData]
    public required string Summary { get; set; }

    [VectorStoreData]
    public required string RootCause { get; set; }

    [VectorStoreData]
    public required string Resolution { get; set; }

    [VectorStoreData]
    public required string Timestamp { get; set; }

    // The embedding of Summary + RootCause + Resolution combined - this is
    // what similarity search actually matches against.
    [VectorStoreVector(1536)] // 1536 = text-embedding-3-small's dimension
    public required ReadOnlyMemory<float> ContentEmbedding { get; set; }
}
