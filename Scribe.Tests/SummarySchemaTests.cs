using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scribe.Models;
using Scribe.Services;

namespace Scribe.Tests;

/// <summary>
/// The schema constrains what the model may emit; TranscriptSummary is what we
/// deserialize into. If they drift, the model is forced to produce something we
/// then silently drop — so these assert they describe the same shape.
/// </summary>
public class SummarySchemaTests
{
    private static IEnumerable<string> JsonNamesOf(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name);

    private static IEnumerable<string> SchemaPropertiesOf(JsonElement node) =>
        node.GetProperty("properties").EnumerateObject().Select(p => p.Name);

    [Fact]
    public void Schema_CoversEveryPropertyOfTranscriptSummary()
    {
        Assert.Equal(
            JsonNamesOf(typeof(TranscriptSummary)).OrderBy(n => n),
            SchemaPropertiesOf(SummarySchema.Schema).OrderBy(n => n));
    }

    [Fact]
    public void Schema_KeyPointMatchesModel()
    {
        var keyPoint = SummarySchema.Schema.GetProperty("properties").GetProperty("keyPoints").GetProperty("items");

        Assert.Equal(
            JsonNamesOf(typeof(SummaryKeyPoint)).OrderBy(n => n),
            SchemaPropertiesOf(keyPoint).OrderBy(n => n));
    }

    [Fact]
    public void Schema_ActionItemMatchesModel()
    {
        var actionItem = SummarySchema.Schema.GetProperty("properties").GetProperty("actionItems").GetProperty("items");

        Assert.Equal(
            JsonNamesOf(typeof(SummaryActionItem)).OrderBy(n => n),
            SchemaPropertiesOf(actionItem).OrderBy(n => n));
    }

    [Fact]
    public void Schema_IsStrict()
    {
        // A permissive schema is still "honoured" by llama.cpp while allowing the
        // fenced, prose-wrapped output this exists to prevent.
        Assert.False(SummarySchema.Schema.GetProperty("additionalProperties").GetBoolean());

        var required = SummarySchema.Schema.GetProperty("required").EnumerateArray().Select(e => e.GetString());
        Assert.Equal(
            JsonNamesOf(typeof(TranscriptSummary)).OrderBy(n => n),
            required.OrderBy(n => n));
    }

    [Fact]
    public void SchemaShapedResponse_DeserializesIntoTranscriptSummary()
    {
        const string response = """
            {
              "oneLiner": "The team agreed to surface in-app activation.",
              "overview": "Two customers could not find the activation control.",
              "keyPoints": [{ "point": "Activation is not discoverable", "turnIndices": [1, 2] }],
              "actionItems": [{ "item": "Design the in-app path", "turnIndices": [7], "assignedTo": "Dana" }]
            }
            """;

        var summary = JsonSerializer.Deserialize<TranscriptSummary>(response, Scribe.Utils.Json.CaseInsensitive)!;

        Assert.Equal("The team agreed to surface in-app activation.", summary.OneLiner);
        Assert.Single(summary.KeyPoints!);
        Assert.Equal([1, 2], summary.KeyPoints![0].TurnIndices);
        Assert.Equal("Dana", summary.ActionItems[0].AssignedTo);
    }

    [Fact]
    public void NullAssignedTo_IsAllowedByTheSchemaAndTheModel()
    {
        const string response = """
            {
              "oneLiner": "x", "overview": "y", "keyPoints": [],
              "actionItems": [{ "item": "Review next week", "turnIndices": [8], "assignedTo": null }]
            }
            """;

        var summary = JsonSerializer.Deserialize<TranscriptSummary>(response, Scribe.Utils.Json.CaseInsensitive)!;

        Assert.Null(summary.ActionItems[0].AssignedTo);
    }
}
