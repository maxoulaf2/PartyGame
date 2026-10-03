using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// The presentation of a quiz question: hidden on the TV screen at first, then shown by the game master as they read it
/// out, the question with its image, then its choices one by one. Opening the answers shows what is still hidden.
/// </summary>
public sealed class QuizPresentationTests
{
    private static readonly string[] _players = ["Zoé", "Max"];

    private static readonly ImmutableArray<QuizRoundDescriptor> _rounds =
        [QuizGames.Round(QuizGames.CapitalQuestion, QuizGames.IllustratedQuestion)];

    [Fact]
    public void Start_Round_HidesTheQuestionFromTheTvScreenButNotFromTheConsole()
    {
        // Given
        var state = QuizGames.Started(_rounds, _players);

        // When
        var display = DisplayOf(state);
        var gameMaster = GameMasterOf(state);

        // Then: the TV screen keeps the room of the four choices, without any text
        Assert.Equal((QuizQuestionPhase.Presentation, null, null, 4), (display.Phase, display.Text, display.ImageUrl, display.ChoiceCount));
        Assert.Empty(display.Choices);
        Assert.Equal((QuizGames.CapitalQuestion.Text, false), (gameMaster.Text, gameMaster.QuestionShown));
        Assert.Equal(["Canberra", "Sydney", "Melbourne", "Perth"], gameMaster.Choices.Select(c => c.Text));
        Assert.All(gameMaster.Choices, choice => Assert.False(choice.Shown));
    }

    [Fact]
    public void ProjectForPlayer_QuestionHidden_ShowsAButtonPerChoice()
    {
        // Given
        var state = QuizGames.Started(_rounds, _players);

        // When
        var view = PlayerOf(state);

        // Then: the phones have shown every button from the start
        Assert.Equal([QuizChoiceLetter.A, QuizChoiceLetter.B, QuizChoiceLetter.C, QuizChoiceLetter.D], view.Choices);
    }

    [Fact]
    public void Handle_ShowQuestion_ShowsItsTextOnTheTvScreenWithoutItsChoices()
    {
        // Given
        var state = QuizGames.Started(_rounds, _players);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowQuestion(state), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        var display = DisplayOf(transition.State);
        Assert.Equal((QuizQuestionPhase.Presentation, QuizGames.CapitalQuestion.Text), (display.Phase, display.Text));
        Assert.Empty(display.Choices);
        var gameMaster = GameMasterOf(transition.State);
        Assert.True(gameMaster.QuestionShown);
        Assert.All(gameMaster.Choices, choice => Assert.False(choice.Shown));
    }

    [Fact]
    public void Handle_ShowQuestion_IllustratedQuestion_ShowsItsImageWithItsText()
    {
        // Given
        var state = QuizGames.AtQuestion(QuizGames.Started(_rounds, _players), 1);
        Assert.Null(DisplayOf(state).ImageUrl);

        // When
        var shown = QuizGames.Accepted(state, QuizGames.ShowQuestion(state));

        // Then
        Assert.Equal(state.Media.UrlOf(QuizGames.Flag), DisplayOf(shown).ImageUrl);
    }

    [Fact]
    public void Handle_ShowQuestionTwice_IsRejected()
    {
        // Given: sent again, or by a second console
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 0);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowQuestion(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PresentationStepMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowQuestion_OtherQuestion_IsRejected()
    {
        // Given
        var state = QuizGames.Started(_rounds, _players);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowQuestion(state, questionNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowQuestion_AnswersOpen_IsRejected()
    {
        // Given
        var state = QuizGames.Answering(QuizGames.Started(_rounds, _players));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowQuestion(state), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowChoice_NextOne_ShowsItOnTheTvScreen()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 0);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A), Games.Context());

        // Then
        Assert.Null(transition.Rejection);
        Assert.Empty(transition.Effects);
        Assert.Equal([new QuizChoiceView(QuizChoiceLetter.A, "Canberra")], DisplayOf(transition.State).Choices);
        Assert.Equal([true, false, false, false], GameMasterOf(transition.State).Choices.Select(c => c.Shown));
        Assert.Equal(QuizQuestionPhase.Presentation, DisplayOf(transition.State).Phase);
    }

    [Fact]
    public void Handle_ShowChoice_OneByOne_ShowsThemInTheOrderOfTheirLetters()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 2);

        // When
        state = QuizGames.Accepted(state, QuizGames.ShowChoice(state, QuizChoiceLetter.C));

        // Then
        Assert.Equal(["Canberra", "Sydney", "Melbourne"], DisplayOf(state).Choices.Select(c => c.Text));
        Assert.Equal([QuizChoiceLetter.A, QuizChoiceLetter.B, QuizChoiceLetter.C], DisplayOf(state).Choices.Select(c => c.Letter));
        Assert.Equal([true, true, true, false], GameMasterOf(state).Choices.Select(c => c.Shown));
    }

    [Fact]
    public void Handle_ShowChoice_ShuffledChoices_ShowsThemInTheOrderShown()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started([_rounds[0] with { ShuffleChoices = true }], _players), choiceCount: 0);
        var first = QuizGames.CapitalQuestion.Choices[QuizGames.RoundOf(state).ChoiceOrder[0]].Text;

        // When
        state = QuizGames.Accepted(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A));

        // Then
        Assert.Equal([new QuizChoiceView(QuizChoiceLetter.A, first)], DisplayOf(state).Choices);
    }

    [Fact]
    public void Handle_ShowChoice_BeforeTheQuestion_IsRejected()
    {
        // Given
        var state = QuizGames.Started(_rounds, _players);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PresentationStepMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowChoice_AlreadyShown_IsRejected()
    {
        // Given: sent again, or by a second console
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 1);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PresentationStepMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowChoice_AfterTheNextOne_IsRejected()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 1);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.C), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PresentationStepMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Theory]
    [InlineData(QuizChoiceLetter.C)]
    [InlineData((QuizChoiceLetter)7)]
    [InlineData((QuizChoiceLetter)(-1))]
    public void Handle_ShowChoice_LetterTheQuestionHasNot_IsRejected(QuizChoiceLetter letter)
    {
        // Given: the illustrated question has two choices, both shown
        var state = QuizGames.Shown(QuizGames.AtQuestion(QuizGames.Started(_rounds, _players), 1));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, letter), Games.Context());

        // Then
        Assert.Equal(RejectionReason.ChoiceUnknown, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowChoice_OtherQuestion_IsRejected()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 0);

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A, questionNumber: 2), Games.Context());

        // Then
        Assert.Equal(RejectionReason.QuestionMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_ShowChoice_AnswersOpen_IsRejected()
    {
        // Given: every choice shows once the answers open
        var state = QuizGames.Answering(QuizGames.Started(_rounds, _players));

        // When
        var transition = QuizGames.Engine.Handle(state, QuizGames.ShowChoice(state, QuizChoiceLetter.A), Games.Context());

        // Then
        Assert.Equal(RejectionReason.PhaseMismatch, transition.Rejection);
        Assert.Same(state, transition.State);
    }

    [Fact]
    public void Handle_OpenAnswers_QuestionHidden_ShowsEverythingAtOnce()
    {
        // Given: the game master opens without showing anything first
        var state = QuizGames.AtQuestion(QuizGames.Started(_rounds, _players), 1);

        // When
        var opened = QuizGames.Accepted(state, QuizGames.OpenAnswers(state));

        // Then
        var display = DisplayOf(opened);
        Assert.Equal((QuizQuestionPhase.Answering, QuizGames.IllustratedQuestion.Text), (display.Phase, display.Text));
        Assert.Equal(state.Media.UrlOf(QuizGames.Flag), display.ImageUrl);
        Assert.Equal(["Japon", "Bangladesh"], display.Choices.Select(c => c.Text));
        Assert.True(GameMasterOf(opened).QuestionShown);
        Assert.All(GameMasterOf(opened).Choices, choice => Assert.True(choice.Shown));
    }

    [Fact]
    public void Handle_OpenAnswers_SomeChoicesShown_ShowsTheOthers()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 2);

        // When
        var opened = QuizGames.Accepted(state, QuizGames.OpenAnswers(state));

        // Then
        Assert.Equal(["Canberra", "Sydney", "Melbourne", "Perth"], DisplayOf(opened).Choices.Select(c => c.Text));
    }

    [Fact]
    public void Handle_NextQuestion_PresentsItHiddenFromTheTvScreen()
    {
        // Given
        var state = QuizGames.Revealed(QuizGames.Shown(QuizGames.Started(_rounds, _players)));

        // When
        var next = QuizGames.Accepted(state, QuizGames.NextQuestion(state));

        // Then
        var display = DisplayOf(next);
        Assert.Equal((2, QuizQuestionPhase.Presentation, null, null, 2), (display.QuestionNumber, display.Phase, display.Text, display.ImageUrl, display.ChoiceCount));
        Assert.Empty(display.Choices);
        Assert.False(GameMasterOf(next).QuestionShown);
    }

    [Fact]
    public void Handle_SkipQuestion_PartlyShown_PresentsTheNextOneHidden()
    {
        // Given
        var state = QuizGames.Shown(QuizGames.Started(_rounds, _players), choiceCount: 3);

        // When
        var next = QuizGames.Accepted(state, QuizGames.SkipQuestion(state));

        // Then
        Assert.Equal((2, null), (DisplayOf(next).QuestionNumber, DisplayOf(next).Text));
        Assert.Empty(DisplayOf(next).Choices);
    }

    private static QuizDisplayView DisplayOf(GameState state) =>
        Assert.IsType<QuizDisplayView>(QuizGames.Snapshots.ForDisplay(state).RoundView);

    private static QuizGameMasterView GameMasterOf(GameState state) =>
        Assert.IsType<QuizGameMasterView>(QuizGames.Snapshots.ForGameMaster(state).RoundView);

    private static QuizPlayerView PlayerOf(GameState state) =>
        Assert.IsType<QuizPlayerView>(QuizGames.Snapshots.ForPlayer(state, state.Players[0]).RoundView);
}
