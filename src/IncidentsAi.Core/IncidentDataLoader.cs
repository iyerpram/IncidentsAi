using System.Reflection;
using System.Text.Json;

namespace IncidentsAi.Core;

public sealed record RawIncident(
    string Id, string Service, string Timestamp, string Severity,
    string Summary, string RootCause, string Resolution)
{
    // Combined text used both for embedding and for what the LLM ultimately reads.
    public string ToContentText() =>
        $"Service: {Service}. Severity: {Severity}. Summary: {Summary}. " +
        $"Root cause: {RootCause}. Resolution: {Resolution}.";
}

// Deliberately has no dependency on Qdrant, OpenAI, or any I/O beyond reading
// its own embedded resource - this is what makes it unit-testable without
// mocking external services. See IncidentsAi.Tests for coverage.
public static class IncidentDataLoader
{
    public static List<RawIncident> LoadSampleIncidents()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "IncidentsAi.Core.SampleData.incidents.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Check the " +
                "EmbeddedResource entry in IncidentsAi.Core.csproj.");

        var json = new StreamReader(stream).ReadToEnd();

        return JsonSerializer.Deserialize<List<RawIncident>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Could not parse incidents.json");
    }
}
