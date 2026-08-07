using Microsoft.Extensions.AI;
using Scribe.Models;
using Scribe.Services;

namespace Scribe.Tests;

public class SummaryServiceTests
{
    /// <summary>Returns whatever the test hands it, so the guards can be exercised without a model.</summary>
    private sealed class StubChatClient(string response) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private static Transcript ThreeTurnTranscript() => new()
    {
        Turns =
        [
            new TranscriptTurn { Id = "T000", SpeakerName = "Speaker 1", Text = "One" },
            new TranscriptTurn { Id = "T001", SpeakerName = "Speaker 2", Text = "Two" },
            new TranscriptTurn { Id = "T002", SpeakerName = "Speaker 1", Text = "Three" }
        ]
    };

    private static async Task<TranscriptSummary> SummarizeAsync(string modelResponse) =>
        await new SummaryService(new StubChatClient(modelResponse)).GenerateSummaryAsync(ThreeTurnTranscript());

    [Fact]
    public async Task CitationsToTurnsThatDoNotExist_AreDropped()
    {
        // T009 is not in a three-turn transcript. A citation that looks resolvable
        // and is not is worse than no citation.
        var summary = await SummarizeAsync("""
            {
              "oneLiner": "x", "abstract": "y", "decisions": [], "openQuestions": [], "actionItems": [],
              "keyPoints": [{ "point": "Something was said", "turnIds": ["T001", "T009"] }]
            }
            """);

        Assert.Equal(["T001"], summary.KeyPoints![0].TurnIds);
    }

    [Fact]
    public async Task ClaimKeepsItsText_EvenWhenEveryCitationIsDropped()
    {
        var summary = await SummarizeAsync("""
            {
              "oneLiner": "x", "abstract": "y", "decisions": [], "openQuestions": [], "actionItems": [],
              "keyPoints": [{ "point": "Still worth keeping", "turnIds": ["T042"] }]
            }
            """);

        Assert.Equal("Still worth keeping", summary.KeyPoints![0].Point);
        Assert.Empty(summary.KeyPoints[0].TurnIds);
    }

    [Fact]
    public async Task DecisionsAndOpenQuestions_AreValidatedToo()
    {
        var summary = await SummarizeAsync("""
            {
              "oneLiner": "x", "abstract": "y", "actionItems": [],
              "decisions": [{ "decision": "Ship it", "rationale": null, "turnIds": ["T002", "T077"] }],
              "openQuestions": [{ "question": "Who owns it?", "turnIds": ["T088"] }],
              "keyPoints": []
            }
            """);

        Assert.Equal(["T002"], summary.Decisions[0].TurnIds);
        Assert.Equal("D-T002", summary.Decisions[0].BaseId);
        Assert.Empty(summary.OpenQuestions[0].TurnIds);
    }

    [Fact]
    public async Task CitationsWrittenIntoProse_AreStripped()
    {
        // The writer renders citations from turnIds; one in the text would double it.
        var summary = await SummarizeAsync("""
            {
              "oneLiner": "x", "abstract": "The team agreed [T001] to proceed.", "decisions": [],
              "openQuestions": [], "actionItems": [],
              "keyPoints": [{ "point": "Agreement reached (turn 1)", "turnIds": ["T001"] }]
            }
            """);

        Assert.Equal("The team agreed to proceed.", summary.Abstract);
        Assert.Equal("Agreement reached", summary.KeyPoints![0].Point);
    }

    [Fact]
    public async Task FencedJson_IsStillRead()
    {
        // Only reachable when a server ignores the response format it accepted.
        var summary = await SummarizeAsync("""
            ```json
            {
              "oneLiner": "Fenced anyway", "abstract": "y", "decisions": [],
              "openQuestions": [], "actionItems": [], "keyPoints": []
            }
            ```
            """);

        Assert.Equal("Fenced anyway", summary.OneLiner);
    }

    [Fact]
    public async Task EmptyResponse_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => SummarizeAsync("   "));
    }
}
