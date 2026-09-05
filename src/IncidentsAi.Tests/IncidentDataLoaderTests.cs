using IncidentsAi.Core;
using IncidentsAi.Core.Models;
using Xunit;

namespace IncidentsAi.Tests;

public class IncidentDataLoaderTests
{
    [Fact]
    public void LoadSampleIncidents_ReturnsAllIncidents()
    {
        var incidents = IncidentDataLoader.LoadSampleIncidents();

        Assert.NotEmpty(incidents);
        Assert.All(incidents, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.Id));
            Assert.False(string.IsNullOrWhiteSpace(i.Service));
            Assert.False(string.IsNullOrWhiteSpace(i.Summary));
        });
    }

    [Fact]
    public void LoadSampleIncidents_IdsAreUnique()
    {
        var incidents = IncidentDataLoader.LoadSampleIncidents();
        var ids = incidents.Select(i => i.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ToContentText_IncludesKeyFields()
    {
        var incident = new RawIncident(
            Id: "INC-9999", Service: "TestService", Timestamp: "2026-01-01T00:00:00Z",
            Severity: "High", Summary: "Something broke", RootCause: "A bug",
            Resolution: "Fixed the bug");

        var text = incident.ToContentText();

        Assert.Contains("TestService", text);
        Assert.Contains("Something broke", text);
        Assert.Contains("A bug", text);
        Assert.Contains("Fixed the bug", text);
    }
}

public class IncidentSearchToolFormattingTests
{
    [Fact]
    public void FormatMatch_IncludesIncidentIdAndKeyDetails()
    {
        var record = new IncidentRecord
        {
            Id = "INC-1001",
            IncidentId = "INC-1001",
            Service = "OrderService",
            Severity = "High",
            Summary = "Error rate spiked.",
            RootCause = "Retry storm.",
            Resolution = "Added circuit breaker.",
            Timestamp = "2026-08-20T09:14:00Z",
            ContentEmbedding = new float[1536] // zeroed - not exercised by formatting
        };

        var formatted = IncidentSearchTool.FormatMatch(record);

        Assert.Contains("INC-1001", formatted);
        Assert.Contains("OrderService", formatted);
        Assert.Contains("Retry storm.", formatted);
        Assert.Contains("Added circuit breaker.", formatted);
    }
}
