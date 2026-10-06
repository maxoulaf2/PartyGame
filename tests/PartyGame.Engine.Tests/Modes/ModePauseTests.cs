using System.Text.Json;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.BlindTest;
using PartyGame.Engine.Modes.Buzzer;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.Projections;
using PartyGame.Engine.Tests.Modes.BlindTest;
using PartyGame.Engine.Tests.Modes.Buzzer;
using PartyGame.Engine.Tests.Modes.OpenQuestion;
using PartyGame.Engine.Tests.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes;

/// <summary>
/// Pause of a round of each game mode, in each of its phases: the timers are cancelled, and the resumption moves the
/// deadlines on by the length of the pause, the same way as the resumption of a game after a restart, which the tests of
/// each mode cover phase by phase.
/// </summary>
public sealed class ModePauseTests
{
    private static readonly string[] _players = ["Zoé", "Max", "Léa"];

    private static readonly GameModes _modes = new([QuizGames.Mode, BuzzerGames.Mode, OpenQuestionGames.Mode, BlindTestGames.Mode]);

    private static readonly GameEngine _engine = new(_modes);

    /// <summary>The persisted form of a state, which compares its collections by value.</summary>
    private static readonly JsonSerializerOptions _json = GameStateJson.CreateOptions(_modes, ContractJsonOptions.Default);

    /// <summary>The game master pauses 3 s after the round started, and resumes 5 minutes later.</summary>
    private static readonly DateTimeOffset _pausedAt = Games.Now.AddSeconds(3);

    private static readonly DateTimeOffset _resumedAt = _pausedAt.AddMinutes(5);

    [Fact]
    public void PauseThenResume_EveryQuizPhase_ResumesAsAfterARestart()
    {
        var started = QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.LastQuestion)], _players);
        AssertEveryPhase<QuizPhase>(
            state => QuizGames.RoundOf(state).Phase,
            started,
            QuizGames.Answering(started, (1, QuizChoiceLetter.A)),
            QuizGames.Locked(started, (1, QuizChoiceLetter.A)),
            QuizGames.Revealed(started, (1, QuizChoiceLetter.A)));
    }

    [Fact]
    public void PauseThenResume_EveryBuzzerPhase_ResumesAsAfterARestart()
    {
        var started = BuzzerGames.Started(BuzzerGames.Round(BuzzerGames.PaintingQuestion, BuzzerGames.LastQuestion), _players);
        AssertEveryPhase<BuzzerPhase>(
            state => BuzzerGames.RoundOf(state).Phase,
            started,
            BuzzerGames.Asked(started),
            BuzzerGames.Buzzed(started, (1, 1000)),
            BuzzerGames.Answering(started, (1, 1000)),
            BuzzerGames.Judged(started, false, 1, 2, 3),
            BuzzerGames.Accepted(BuzzerGames.Asked(started), BuzzerGames.RevealAnswer(BuzzerGames.Asked(started))));
    }

    [Fact]
    public void PauseThenResume_EveryOpenQuestionPhase_ResumesAsAfterARestart()
    {
        var started = OpenQuestionGames.Started([OpenQuestionGames.Round(OpenQuestionGames.PaintingQuestion, OpenQuestionGames.YearQuestion)], _players);
        AssertEveryPhase<OpenQuestionPhase>(
            state => OpenQuestionGames.RoundOf(state).Phase,
            started,
            OpenQuestionGames.Answering(started, (1, "Vinci")),
            OpenQuestionGames.Locked(started, (1, "Vinci")),
            OpenQuestionGames.Judged(started, [1], (1, "Vinci")),
            OpenQuestionGames.Revealed(started, [1], (1, "Vinci")));
    }

    [Fact]
    public void PauseThenResume_EveryBlindTestPhase_ResumesAsAfterARestart()
    {
        var started = BlindTestGames.Started(BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon), _players);
        AssertEveryPhase<BlindTestPhase>(
            state => BlindTestGames.RoundOf(state).Phase,
            started,
            BlindTestGames.Played(started),
            BlindTestGames.Buzzed(started, (1, 1000)),
            BlindTestGames.Answering(started, (1, 1000)),
            BlindTestGames.Accepted(BlindTestGames.Played(started), BlindTestGames.RevealAnswer(BlindTestGames.Played(started))));
    }

    [Fact]
    public void Resume_QuizPausedTwelveSecondsBeforeTheEnd_GoesOnWithTwelveSeconds()
    {
        // Given: a countdown of 20 s, started at Now, paused 8 s in
        var state = QuizGames.Shown(QuizGames.Started([QuizGames.Round(QuizGames.CapitalQuestion) with { AnswerSeconds = 20 }], _players));
        var paused = Handle(state, Pause(state), Games.Now.AddSeconds(8));

        // When
        var resumed = Handle(paused, Resume(paused), _resumedAt);

        // Then
        Assert.Equal(_resumedAt.AddSeconds(12), QuizGames.RoundOf(resumed).AnswersCloseAt);
    }

    [Fact]
    public void ProjectForDisplay_MusicPausedWithTheGame_StandsStillWhereThePauseStoppedIt()
    {
        // Given: the excerpt plays from its start, paused 3 s in
        var state = BlindTestGames.Played(BlindTestGames.Started(BlindTestGames.Round(BlindTestGames.Ode), _players));
        var startsAt = BlindTestGames.StartsAt(state);
        var paused = Handle(state, Pause(state), startsAt.AddSeconds(3));

        // When
        var view = Assert.IsType<BlindTestDisplayView>(new Snapshots(_modes).ForDisplay(paused).RoundView);

        // Then
        Assert.Equal((3, null), (view.Playback.Position, view.Playback.StartsAt));
    }

    [Fact]
    public void Resume_MusicPausedWithTheGame_GoesOnFromWhereItStopped()
    {
        // Given: paused 3 s into the excerpt
        var state = BlindTestGames.Played(BlindTestGames.Started(BlindTestGames.Round(BlindTestGames.Ode), _players));
        var startsAt = BlindTestGames.StartsAt(state);
        var paused = Handle(state, Pause(state), startsAt.AddSeconds(3));

        // When
        var resumed = Handle(paused, Resume(paused), _resumedAt);

        // Then: 3 s into the excerpt at the resumption
        Assert.Equal(new ExcerptPlayback(0, _resumedAt.AddSeconds(-3)), BlindTestGames.RoundOf(resumed).Playback);
    }

    [Fact]
    public void ProjectForDisplay_MusicPausedOnceTheExcerptEnded_StandsStillAtItsEnd()
    {
        // Given: an excerpt of 20 s, paused 25 s in
        var state = BlindTestGames.Played(BlindTestGames.Started(BlindTestGames.Round(BlindTestGames.Ode), _players));
        var paused = Handle(state, Pause(state), BlindTestGames.StartsAt(state).AddSeconds(25));

        // When
        var view = Assert.IsType<BlindTestDisplayView>(new Snapshots(_modes).ForDisplay(paused).RoundView);

        // Then
        Assert.Equal((20, null), (view.Playback.Position, view.Playback.StartsAt));
    }

    [Fact]
    public void Handle_BuzzWhilePaused_IsRejected()
    {
        // Given: the buzzer opened, and the game paused before the buzz arrived
        var state = BuzzerGames.Asked(BuzzerGames.Started(BuzzerGames.Round(BuzzerGames.PaintingQuestion), _players));
        var paused = Handle(state, Pause(state), _pausedAt);

        // When: the buzz was pressed before the pause
        var transition = _engine.Handle(paused, BuzzerGames.Buzz(paused, 1, Games.Now.AddSeconds(1)), At(_pausedAt.AddSeconds(1)));

        // Then
        Assert.Equal(RejectionReason.GamePaused, transition.Rejection);
    }

    /// <summary>
    /// Fails if <paramref name="states"/> leave a phase of <typeparamref name="TPhase"/> uncovered, or if one of them,
    /// paused then resumed, is not as after a restart that lasted as long as the pause.
    /// </summary>
    private static void AssertEveryPhase<TPhase>(Func<GameState, TPhase> phaseOf, params GameState[] states)
        where TPhase : struct, Enum
    {
        Assert.Equal(Enum.GetValues<TPhase>().Order(), states.Select(phaseOf).Distinct().Order());
        foreach (var state in states)
        {
            var paused = _engine.Handle(state, Pause(state), At(_pausedAt));
            Assert.Equal([new CancelRoundTimers(state.CurrentRound!.Id)], paused.Effects);

            var resumed = _engine.Handle(paused.State, Resume(paused.State), At(_resumedAt));
            var restarted = _engine.Handle(state, new GameResumed(_pausedAt), At(_resumedAt));
            Assert.Null(resumed.Rejection);
            Assert.Equal(JsonSerializer.Serialize(restarted.State, _json), JsonSerializer.Serialize(resumed.State, _json));
            Assert.Equal(restarted.Effects, resumed.Effects);
        }
    }

    private static GameState Handle(GameState state, GameInput input, DateTimeOffset now)
    {
        var transition = _engine.Handle(state, input, At(now));
        Assert.Null(transition.Rejection);
        return transition.State;
    }

    private static PauseGame Pause(GameState state) => new(state.GameId, Paused: true, Games.Now);

    private static PauseGame Resume(GameState state) => new(state.GameId, Paused: false, Games.Now);

    private static GameContext At(DateTimeOffset now) => new(now, new Random(42));
}
