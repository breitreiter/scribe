using System.Text.Json;

namespace Scribe.Services;

/// <summary>
/// JSON schema for <see cref="Models.TranscriptSummary"/>, used to constrain decoding.
///
/// Hand-written rather than reflected so the wire contract is reviewable in one place;
/// SummarySchemaTests asserts it cannot drift from the model. `additionalProperties: false`
/// and a complete `required` list are what make llama.cpp's grammar strict — a partial
/// schema is honoured but permissive, which reintroduces the failure it exists to prevent.
/// </summary>
public static class SummarySchema
{
    public const string Name = "transcript_summary";

    public static JsonElement Schema { get; } = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "oneLiner": {
              "type": "string",
              "description": "A single sentence (max 20 words) summarizing the entire meeting"
            },
            "overview": {
              "type": "string",
              "description": "A 2-3 paragraph overview of the meeting discussion"
            },
            "keyPoints": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "point": { "type": "string" },
                  "turnIndices": { "type": "array", "items": { "type": "integer" } }
                },
                "required": ["point", "turnIndices"],
                "additionalProperties": false
              }
            },
            "actionItems": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "item": { "type": "string" },
                  "turnIndices": { "type": "array", "items": { "type": "integer" } },
                  "assignedTo": { "type": ["string", "null"] }
                },
                "required": ["item", "turnIndices", "assignedTo"],
                "additionalProperties": false
              }
            }
          },
          "required": ["oneLiner", "overview", "keyPoints", "actionItems"],
          "additionalProperties": false
        }
        """).RootElement;
}
