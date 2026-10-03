using System.Collections.Immutable;
using System.Text.Json;
using PartyGame.Contracts.Serialization;
using PartyGame.Tests.Shared.Leaks;
using Xunit.Sdk;

namespace PartyGame.Engine.Tests.Leaks;

/// <summary>
/// Tests of the shared leak tool on a toy mode, whose projections each test makes leak on purpose.
/// </summary>
public sealed class LeakSuiteTests
{
    private const string Token = "token-of-zoe-7f3a";

    private static readonly string[] _cities = ["Paris", "Lyon"];

    private static ToyState Question => new(ToyPhase.Question, "Paris", Choices: ImmutableDictionary<string, string>.Empty.Add("Zoé", "Marseille"));

    private static ToyState Reveal => Question with { Phase = ToyPhase.Reveal };

    private enum ToyPhase
    {
        Question,
        Reveal,
    }

    [Fact]
    public void AssertNoSecretIsShown_NoLeak_Passes()
    {
        // Given
        var suite = Suite(Fair);

        // When / Then
        suite.AssertNoSecretIsShown();
        suite.AssertPairsLookTheSame();
        suite.AssertEveryPhaseIsCovered();
    }

    [Fact]
    public void AssertNoSecretIsShown_AnswerOnTheDisplayBeforeTheReveal_NamesScenarioPhaseRoleAndPath()
    {
        // Given
        var suite = Suite(state => Fair(state, display: new { phase = state.Phase, question = new { answer = state.Answer } }));

        // When
        var message = FailureOf(suite.AssertNoSecretIsShown);

        // Then
        Assert.Contains(
            "Scenario \"question\" (phase Question), Display: \"Paris\" (hidden from all but the game master) found at $.question.answer",
            message,
            StringComparison.Ordinal);
        Assert.DoesNotContain("phase Reveal", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoSecretIsShown_ChoiceOfAPlayerOnTheirOwnPhoneOnly_Passes()
    {
        // Given: Zoé sees her own choice, Max does not see it
        var suite = Suite(Fair);

        // When / Then
        suite.AssertNoSecretIsShown();
    }

    [Fact]
    public void AssertNoSecretIsShown_ChoiceOfAnotherPlayer_NamesThePlayerWhoSeesIt()
    {
        // Given
        var suite = Suite(state => Fair(state, maxPhone: new { phase = state.Phase, others = state.Choices }));

        // When
        var message = FailureOf(suite.AssertNoSecretIsShown);

        // Then: the nickname, a dictionary key, is data too
        Assert.Contains("Player Max: \"Marseille\" (hidden from all but the game master and Zoé) found at $.others['Zoé']", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Player Zoé:", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoSecretIsShown_SecretInAPropertyName_IsFound()
    {
        // Given
        var suite = Suite(state => Fair(state, display: new Dictionary<string, int> { [state.Answer] = 1 }));

        // When
        var message = FailureOf(suite.AssertNoSecretIsShown);

        // Then
        Assert.Contains("found at $.Paris (property name)", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoSecretIsShown_NonAsciiSecret_IsFoundDespiteItsEscaping()
    {
        // Given: the serializer escapes non-ASCII characters
        Assert.Contains("\\u00C9chauffement", JsonSerializer.Serialize("Échauffement", ContractJsonOptions.Default), StringComparison.Ordinal);
        var secret = Question with { Answer = "Échauffement" };
        var suite = Suite(state => Fair(state, display: new { answer = state.Answer }), scenarios: [("question", secret)]);

        // When
        var message = FailureOf(suite.AssertNoSecretIsShown);

        // Then
        Assert.Contains("\"Échauffement\" (hidden from all but the game master) found at $.answer", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertNoSecretIsShown_AnswerOnTheGameMasterConsole_Passes()
    {
        // Given: the game master is the only one allowed to see it before the reveal
        var suite = Suite(state => Fair(state, gameMaster: new { answer = state.Answer, choices = state.Choices }));

        // When / Then
        suite.AssertNoSecretIsShown();
    }

    [Fact]
    public void AssertNoSecretIsShown_TokenOnTheGameMasterConsole_Fails()
    {
        // Given
        var suite = Suite(state => Fair(state, gameMaster: new { token = Token }));

        // When
        var message = FailureOf(suite.AssertNoSecretIsShown);

        // Then
        Assert.Contains($"GameMaster: \"{Token}\" (hidden from everyone) found at $.token", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertPairsLookTheSame_IndexOfTheAnswerOnTheDisplay_NamesThePathOfTheFirstDifference()
    {
        // Given: no text of the answer is shown, but its position is
        var suite = Suite(state => Fair(state, display: new { phase = state.Phase, choices = new[] { new { correct = state.Answer == "Paris" } } }));

        // When
        var message = FailureOf(suite.AssertPairsLookTheSame);

        // Then
        Assert.Contains(
            "Pair \"correct answer\" (phase Question), Display: $.choices[0].correct is true in one state, false in the other",
            message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AssertPairsLookTheSame_OrderOfTheChoicesFollowsTheAnswer_NamesTheFirstDifferentItem()
    {
        // Given
        var suite = Suite(state => Fair(state, display: new { choices = _cities.OrderBy(c => c != state.Answer).ToArray() }));

        // When
        var message = FailureOf(suite.AssertPairsLookTheSame);

        // Then
        Assert.Contains("Display: $.choices[0] is \"Paris\" in one state, \"Lyon\" in the other", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertPairsLookTheSame_PropertyOnlyInOneState_NamesIt()
    {
        // Given
        var suite = Suite(state => Fair(
            state,
            display: state.Answer == "Paris" ? new Dictionary<string, int> { ["count"] = 1, ["hint"] = 2 } : new Dictionary<string, int> { ["count"] = 1 }));

        // When
        var message = FailureOf(suite.AssertPairsLookTheSame);

        // Then
        Assert.Contains("Display: $.hint is only in one state", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertPairsLookTheSame_ChoiceShownOnTheOwnPhoneOfThePlayer_Passes()
    {
        // Given
        var pair = new SecretPair<ToyState>("choice of Zoé", Question, Question with { Choices = Question.Choices.SetItem("Zoé", "Nice") }, Audience.AllButGameMasterAnd("Zoé"));
        var suite = Suite(Fair, pairs: [pair]);

        // When / Then
        suite.AssertPairsLookTheSame();
    }

    [Fact]
    public void AssertPairsLookTheSame_ChoiceShownOnAnotherPhone_NamesThatPlayer()
    {
        // Given
        var pair = new SecretPair<ToyState>("choice of Zoé", Question, Question with { Choices = Question.Choices.SetItem("Zoé", "Nice") }, Audience.AllButGameMasterAnd("Zoé"));
        var suite = Suite(state => Fair(state, maxPhone: new { count = state.Choices.Values.Count(c => c == "Nice") }), pairs: [pair]);

        // When
        var message = FailureOf(suite.AssertPairsLookTheSame);

        // Then
        Assert.Contains("Pair \"choice of Zoé\" (phase Question), Player Max: $.count is 0 in one state, 1 in the other", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertPairsLookTheSame_StatesInDifferentPhases_Fails()
    {
        // Given
        var suite = Suite(Fair, pairs: [new SecretPair<ToyState>("correct answer", Question, Reveal, Audience.AllButGameMaster)]);

        // When
        var message = FailureOf(suite.AssertPairsLookTheSame);

        // Then
        Assert.Contains("its states are in phases Question and Reveal", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertEveryPhaseIsCovered_PhaseWithoutScenario_NamesItForEachRole()
    {
        // Given: the pair is in the question phase too
        var suite = Suite(Fair, scenarios: [("question", Question)]);

        // When
        var message = FailureOf(suite.AssertEveryPhaseIsCovered);

        // Then
        Assert.Contains("Phase Reveal, Player: no scenario covers it", message, StringComparison.Ordinal);
        Assert.Contains("Phase Reveal, Display: no scenario covers it", message, StringComparison.Ordinal);
        Assert.Contains("Phase Reveal, GameMaster: no scenario covers it", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Phase Question", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertEveryPhaseIsCovered_ScenarioWithoutPlayer_NamesThePlayerRole()
    {
        // Given
        var suite = Suite(
            state => state.Phase == ToyPhase.Reveal ? new ProjectionSet(new { }, new { }, []) : Fair(state),
            scenarios: [("question", Question), ("reveal", Reveal)]);

        // When
        var message = FailureOf(suite.AssertEveryPhaseIsCovered);

        // Then
        Assert.Contains("Phase Reveal, Player: no scenario covers it", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Phase Reveal, Display", message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSecretReceived_SecretInASnapshot_NamesItsVersionAndPhase()
    {
        // Given
        string[] json = ["""{"version":3,"phase":"Lobby","players":[]}""", $$"""{"version":4,"phase":"Lobby","players":[{"token":"{{Token}}"}]}"""];

        // When
        var message = FailureOf(() => LeakAssert.NoSecretReceived(Viewer.Display, json, new Secret(Token, Audience.Everyone)));

        // Then
        Assert.Contains($"Snapshot version 4 (phase Lobby), Display: \"{Token}\" (hidden from everyone) found at $.players[0].token", message, StringComparison.Ordinal);
        Assert.DoesNotContain("version 3", message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSecretReceived_NoSnapshot_Fails()
    {
        // When
        var message = FailureOf(() => LeakAssert.NoSecretReceived(Viewer.PhoneOf("Zoé"), [], new Secret(Token, Audience.Everyone)));

        // Then
        Assert.Contains("Player Zoé received no snapshot", message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Projections that show nothing they should not: the answer to the game master, then to everyone at the reveal, and
    /// the choice of a player to themselves. A test replaces one of them with a leaky one.
    /// </summary>
    private static ProjectionSet Fair(ToyState state) => Fair(state, display: null);

    private static ProjectionSet Fair(ToyState state, object? display = null, object? gameMaster = null, object? maxPhone = null)
    {
        var answer = state.Phase == ToyPhase.Reveal ? state.Answer : null;
        return new ProjectionSet(
            display ?? new { phase = state.Phase, answer },
            gameMaster ?? new { phase = state.Phase, answer = state.Answer },
            [
                ("Zoé", new { phase = state.Phase, answer, choice = state.Choices.GetValueOrDefault("Zoé") }),
                ("Max", maxPhone ?? new { phase = state.Phase, answer, choice = state.Choices.GetValueOrDefault("Max") }),
            ]);
    }

    private static LeakSuite<ToyState, ToyPhase> Suite(
        Func<ToyState, ProjectionSet> project,
        IReadOnlyList<(string, ToyState)>? scenarios = null,
        IReadOnlyList<SecretPair<ToyState>>? pairs = null) =>
        new()
        {
            PhaseOf = state => state.Phase,
            Project = project,
            Scenarios = scenarios ?? [("question", Question), ("reveal", Reveal)],
            SecretsOf = SecretsOf,
            Pairs = pairs ?? [new SecretPair<ToyState>("correct answer", Question, Question with { Answer = "Lyon" }, Audience.AllButGameMaster)],
        };

    private static IEnumerable<Secret> SecretsOf(ToyState state)
    {
        yield return new Secret(Token, Audience.Everyone);
        if (state.Phase == ToyPhase.Question)
        {
            yield return new Secret(state.Answer, Audience.AllButGameMaster);
            foreach (var (player, choice) in state.Choices)
            {
                yield return new Secret(choice, Audience.AllButGameMasterAnd(player));
            }
        }
    }

    private static string FailureOf(Action assertion) => Assert.ThrowsAny<XunitException>(assertion).Message;

    private sealed record ToyState(ToyPhase Phase, string Answer, ImmutableDictionary<string, string> Choices);
}
