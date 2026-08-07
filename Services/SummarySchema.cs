using System.Text.Json;

namespace Scribe.Services;

/// <summary>
/// JSON schema for <see cref="Models.TranscriptSummary"/>, used to constrain decoding.
///
/// Hand-written rather than reflected so the wire contract is reviewable in one place;
/// SummarySchemaTests asserts it cannot drift from the model. `additionalProperties: false`
/// and a complete `required` list are what make llama.cpp's grammar strict — a partial
/// schema is honoured but permissive, which reintroduces the failure it exists to prevent.
///
/// Turn references are string IDs ("T017"), never positional indices: indices break the
/// moment anything is re-segmented, and a citation that cannot be resolved is worse than
/// no citation.
/// </summary>
public static class SummarySchema
{
    public const string Name = "transcript_summary";

    private const string TurnIds =
        """{ "type": "array", "items": { "type": "string", "pattern": "^T[0-9]+$" } }""";

    public static JsonElement Schema { get; } = JsonDocument.Parse(
        $$"""
        {
          "type": "object",
          "properties": {
            "oneLiner": {
              "type": "string",
              "description": "A single sentence (max 20 words) summarizing the entire meeting"
            },
            "abstract": {
              "type": "string",
              "description": "One dense paragraph covering what happened and what came of it"
            },
            "decisions": {
              "type": "array",
              "description": "Things the meeting settled. Empty array if none were.",
              "items": {
                "type": "object",
                "properties": {
                  "decision": { "type": "string" },
                  "rationale": { "type": ["string", "null"] },
                  "turnIds": {{TurnIds}}
                },
                "required": ["decision", "rationale", "turnIds"],
                "additionalProperties": false
              }
            },
            "actionItems": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "item": { "type": "string" },
                  "turnIds": {{TurnIds}},
                  "assignedTo": { "type": ["string", "null"] }
                },
                "required": ["item", "turnIds", "assignedTo"],
                "additionalProperties": false
              }
            },
            "openQuestions": {
              "type": "array",
              "description": "Raised and left unresolved. Empty array if none were.",
              "items": {
                "type": "object",
                "properties": {
                  "question": { "type": "string" },
                  "turnIds": {{TurnIds}}
                },
                "required": ["question", "turnIds"],
                "additionalProperties": false
              }
            },
            "keyPoints": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "point": { "type": "string" },
                  "turnIds": {{TurnIds}}
                },
                "required": ["point", "turnIds"],
                "additionalProperties": false
              }
            }
          },
          "required": ["oneLiner", "abstract", "decisions", "actionItems", "openQuestions", "keyPoints"],
          "additionalProperties": false
        }
        """).RootElement;
}
