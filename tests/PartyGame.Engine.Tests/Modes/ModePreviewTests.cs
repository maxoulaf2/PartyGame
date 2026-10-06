using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.BlindTest;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine.Audio;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Projections;
using PartyGame.Engine.Tests.Modes.BlindTest;
using PartyGame.Engine.Tests.Modes.Buzzer;
using PartyGame.Engine.Tests.Modes.OpenQuestion;
using PartyGame.Engine.Tests.Modes.Quiz;

namespace PartyGame.Engine.Tests.Modes;

/// <summary>
/// The preview of a pack of every mode on the TV screen: each step revealed as the TV screen shows it at the end of a game,
/// without any player.
/// </summary>
public sealed class ModePreviewTests
{
    private static readonly GameModes _modes = new([QuizGames.Mode, BuzzerGames.Mode, BlindTestGames.Mode, OpenQuestionGames.Mode]);

    private static readonly GameEngine _engine = new(_modes);

    private static readonly Snapshots _snapshots = new(_modes);

    private static readonly ImmutableArray<RoundDescriptor> _rounds =
    [
        QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion),
        BuzzerGames.Round(BuzzerGames.PaintingQuestion),
        BlindTestGames.Round(BlindTestGames.Ode, BlindTestGames.Moon),
        OpenQuestionGames.Round(OpenQuestionGames.FlagQuestion),
    ];

    private static readonly CatalogPack _pack = Games.ValidPack("tous-les-modes", "Tous les modes", _rounds) with
    {
        Media =
        [
            QuizGames.Flag,
            BlindTestGames.Ode.Excerpt.File,
            BlindTestGames.Moon.Excerpt.File,
            BlindTestGames.Moon.Image!.Value,
            OpenQuestionGames.Flag,
        ],
    };

    [Fact]
    public void Preview_QuizQuestion_ShowsItRevealedWithEveryChoice()
    {
        // When
        var (state, preview) = Previewed(round: 1, step: 2);

        // Then
        var view = Assert.IsType<QuizDisplayView>(preview.View);
        Assert.Equal(new RoundStep(2, 2), preview.Step);
        Assert.Equal(QuizQuestionPhase.Revealed, view.Phase);
        Assert.Equal(QuizGames.IllustratedQuestion.Text, view.Text);
        Assert.Equal(state.Preview!.Media.UrlOf(QuizGames.Flag), view.ImageUrl);
        Assert.Equal(["Japon", "Bangladesh"], view.Choices.Select(c => c.Text));
        Assert.Equal(QuizChoiceLetter.A, view.Reveal!.CorrectChoice);
        Assert.Empty(view.Reveal.Answers);
        Assert.False(_snapshots.ForGameMaster(state).Preview!.HasExcerpt);
    }

    [Fact]
    public void Preview_BuzzerQuestion_ShowsItWithItsAnswer()
    {
        // When
        var (_, preview) = Previewed(round: 2, step: 1);

        // Then
        var view = Assert.IsType<BuzzerDisplayView>(preview.View);
        Assert.Equal(new RoundStep(1, 1), preview.Step);
        Assert.Equal(BuzzerQuestionPhase.Revealed, view.Phase);
        Assert.Equal(BuzzerGames.PaintingQuestion.Text, view.Text);
        Assert.Equal(BuzzerGames.PaintingQuestion.Answer, view.Answer);
        Assert.Null(view.FoundBy);
    }

    [Fact]
    public void Preview_BlindTestTrack_ShowsItRevealedWithItsExcerptStandingStill()
    {
        // When
        var (state, preview) = Previewed(round: 3, step: 2);

        // Then
        var view = Assert.IsType<BlindTestDisplayView>(preview.View);
        Assert.Equal(new RoundStep(2, 2), preview.Step);
        Assert.Equal(BlindTestTrackPhase.Revealed, view.Phase);
        Assert.Equal((BlindTestGames.Moon.Title, BlindTestGames.Moon.Artist), (view.Title, view.Artist));
        Assert.Equal(state.Preview!.Media.UrlOf(BlindTestGames.Moon.Image!.Value), view.ImageUrl);
        Assert.Equal(new AudioPlayback(state.Preview.Media.UrlOf(BlindTestGames.Moon.Excerpt.File), 9.5, 24.5, StartsAt: null), view.Playback);
        Assert.True(_snapshots.ForGameMaster(state).Preview!.HasExcerpt);
    }

    [Fact]
    public void Preview_BlindTestExcerptPlayed_PlaysFromItsStart()
    {
        // When
        var (_, preview) = Previewed(round: 3, step: 2, playExcerpt: true);

        // Then
        var playback = Assert.IsType<BlindTestDisplayView>(preview.View).Playback;
        Assert.Equal(9.5, playback.Position);
        Assert.Equal((Games.Now + ExcerptPlayback.Lead).ToUnixTimeMilliseconds(), playback.StartsAt);
    }

    [Fact]
    public void Preview_OpenQuestion_ShowsItWithItsAnswer()
    {
        // When
        var (state, preview) = Previewed(round: 4, step: 1);

        // Then
        var view = Assert.IsType<OpenQuestionDisplayView>(preview.View);
        Assert.Equal(OpenQuestionQuestionPhase.Revealed, view.Phase);
        Assert.Equal(OpenQuestionGames.FlagQuestion.Text, view.Text);
        Assert.Equal(state.Preview!.Media.UrlOf(OpenQuestionGames.Flag), view.ImageUrl);
        Assert.Equal(OpenQuestionGames.FlagQuestion.Answer, view.Reveal!.ExpectedAnswer);
        Assert.Empty(view.Reveal.Groups);
        Assert.Empty(view.Reveal.WithoutAnswer);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    public void Handle_StepPastTheLastOfTheRound_IsRejected(int round, int step)
    {
        // Given
        var (state, _) = Previewed(round: 1, step: 1);

        // When
        var transition = _engine.Handle(state, new ShowPreviewStep(round, step, PlayExcerpt: false, Games.Now), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PreviewStepUnknown, transition.Rejection);
    }

    /// <summary>
    /// A lobby of one player where the pack of every mode is previewed at the given step, and what the TV screen shows.
    /// </summary>
    private static (GameState State, DisplayPreview Preview) Previewed(int round, int step, bool playExcerpt = false)
    {
        var state = Accepted(Games.LobbyWith("Zoé"), Games.Loaded(_pack));
        state = Accepted(state, new StartPreview(_pack.Id, Games.Now));
        if ((round, step, playExcerpt) != (1, 1, false))
        {
            state = Accepted(state, new ShowPreviewStep(round, step, playExcerpt, Games.Now));
        }

        return (state, _snapshots.ForDisplay(state).Preview!);
    }

    private static GameState Accepted(GameState state, GameInput input)
    {
        var transition = _engine.Handle(state, input, Games.Context());
        Assert.Null(transition.Rejection);
        return transition.State;
    }
}
