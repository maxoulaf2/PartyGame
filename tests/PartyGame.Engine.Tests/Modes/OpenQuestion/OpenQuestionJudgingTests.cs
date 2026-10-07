using System.Collections.Immutable;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

/// <summary>
/// The answers of an open question once locked: grouped and pre-classified by the server, then judged in one go by the
/// game master.
/// </summary>
public sealed class OpenQuestionJudgingTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa", "Tom", "Ana"];

    private static readonly ImmutableArray<OpenQuestionRoundDescriptor> _rounds =
        [OpenQuestionGames.Round(OpenQuestionGames.PaintingQuestion, OpenQuestionGames.YearQuestion)];

    [Fact]
    public void Handle_AnswersLock_GroupsIdenticalAnswersOnceNormalized_AcceptedFirstThenToCheckThenRejected()
    {
        // When: Max and Léa typed the same once normalized, Léa and Ana the same text
        var state = OpenQuestionGames.Locked(Presented(), (1, "Picasso"), (2, "léonard de vinci"), (3, "Léonard de Vinci !"), (4, "Leonard de Vinchi"), (5, "Léonard de Vinci !"));

        // Then
        var round = OpenQuestionGames.RoundOf(state);
        Assert.Equal(OpenQuestionPhase.Locked, round.Phase);
        Assert.Equal(
            [
                ("Léonard de Vinci !", OpenQuestionAnswerCategory.Accepted, [2, 3, 5]),
                ("Leonard de Vinchi", OpenQuestionAnswerCategory.ToCheck, [4]),
                ("Picasso", OpenQuestionAnswerCategory.Rejected, (int[])[1]),
            ],
            round.Groups.Select(group => (group.Text, group.Category, group.Players.Select(PlayerNumber).ToArray())));
    }

    [Fact]
    public void Handle_AnswersLock_TieBetweenTexts_KeepsTheTextOfTheFirstAuthor()
    {
        var state = OpenQuestionGames.Locked(Presented(), (2, "de vinci"), (1, "De Vinci"));

        Assert.Equal("De Vinci", Assert.Single(OpenQuestionGames.RoundOf(state).Groups).Text);
    }

    [Fact]
    public void Handle_AnswersLock_WithoutAnyAnswer_JudgesTheQuestionAtOnce()
    {
        var round = OpenQuestionGames.RoundOf(OpenQuestionGames.Locked(Presented()));

        Assert.Equal(OpenQuestionPhase.Judged, round.Phase);
        Assert.Empty(round.Groups);
    }

    [Fact]
    public void Handle_Judge_Locked_RecordsThePlayersAcceptedInTheOrderOfTheParticipants()
    {
        // Given
        var state = OpenQuestionGames.Locked(Presented(), (1, "Picasso"), (2, "Vinci"), (4, "Leonard de Vinchi"));

        // When: the game master accepts the answer to check too, Max named twice
        var transition = OpenQuestionGames.Engine.Handle(state, OpenQuestionGames.Judge(state, [4, 2, 2]), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var round = OpenQuestionGames.RoundOf(transition.State);
        Assert.Equal(OpenQuestionPhase.Judged, round.Phase);
        Assert.Equal([2, 4], round.AcceptedPlayers.Select(PlayerNumber));
    }

    [Fact]
    public void Handle_Judge_NobodyAccepted_IsJudged() =>
        Assert.Equal(OpenQuestionPhase.Judged, OpenQuestionGames.RoundOf(OpenQuestionGames.Judged(Presented(), [], (1, "Picasso"))).Phase);

    [Fact]
    public void Handle_Judge_Twice_IsRejected() =>
        AssertRejected(OpenQuestionGames.Judged(Presented(), [2], (2, "Vinci")), s => OpenQuestionGames.Judge(s, []), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_Judge_BeforeTheAnswersLock_IsRejected() =>
        AssertRejected(OpenQuestionGames.Answering(Presented(), (2, "Vinci")), s => OpenQuestionGames.Judge(s, [2]), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_Judge_Presented_IsRejected() =>
        AssertRejected(Presented(), s => OpenQuestionGames.Judge(s, []), RejectionReason.PhaseMismatch);

    [Fact]
    public void Handle_Judge_OtherQuestion_IsRejected() =>
        AssertRejected(OpenQuestionGames.Locked(Presented(), (2, "Vinci")), s => OpenQuestionGames.Judge(s, [2], questionNumber: 2), RejectionReason.QuestionMismatch);

    [Fact]
    public void Handle_Judge_PlayerWithoutAnswer_IsRejected() =>
        AssertRejected(OpenQuestionGames.Locked(Presented(), (2, "Vinci")), s => OpenQuestionGames.Judge(s, [2, 3]), RejectionReason.PlayerWithoutAnswer);

    [Fact]
    public void Handle_SkipQuestion_Judged_PresentsTheNextOne()
    {
        var state = OpenQuestionGames.Skipped(OpenQuestionGames.Judged(Presented(), [2], (2, "Vinci")));

        var round = OpenQuestionGames.RoundOf(state);
        Assert.Equal((1, OpenQuestionPhase.Presentation), (round.QuestionIndex, round.Phase));
        Assert.Empty(round.Groups);
        Assert.Empty(round.AcceptedPlayers);
    }

    [Fact]
    public void ProjectForGameMaster_Locked_ShowsTheGroupsWithoutVerdict_ThenJudged_WithIt()
    {
        // Given
        var locked = OpenQuestionGames.Locked(Presented(), (1, "Picasso"), (2, "De Vinci"));
        var judged = OpenQuestionGames.Accepted(locked, OpenQuestionGames.Judge(locked, [1]));

        // When
        var before = Assert.IsType<OpenQuestionGameMasterView>(OpenQuestionGames.Snapshots.ForGameMaster(locked).RoundView);
        var after = Assert.IsType<OpenQuestionGameMasterView>(OpenQuestionGames.Snapshots.ForGameMaster(judged).RoundView);

        // Then
        Assert.Equal(
            [("De Vinci", OpenQuestionAnswerCategory.Accepted, (bool?)null), ("Picasso", OpenQuestionAnswerCategory.Rejected, null)],
            before.Groups.Select(group => (group.Text, group.Category, group.Accepted)));
        Assert.Equal([false, true], after.Groups.Select(group => group.Accepted));
        Assert.Equal(OpenQuestionQuestionPhase.Judged, after.Phase);
    }

    private static GameState Presented() => OpenQuestionGames.Started(_rounds, _players);

    private static int PlayerNumber(Contracts.PlayerId id) =>
        Enumerable.Range(1, _players.Length).Single(number => Games.PlayerIdOf(number) == id);

    private static void AssertRejected(GameState state, Func<GameState, GameInput> input, RejectionReason reason)
    {
        var transition = OpenQuestionGames.Engine.Handle(state, input(state), Games.Context());

        Assert.Equal(reason, transition.Rejection);
        Assert.Same(state, transition.State);
        Assert.Empty(transition.Effects);
    }
}
