using System.Collections.Immutable;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.Projections;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.Quiz;

/// <summary>
/// Builds the quiz rounds and the games that play them, with the real quiz mode.
/// </summary>
internal static class QuizGames
{
    public static readonly QuizMode Mode = new();

    public static readonly GameModes Modes = new([Mode]);

    public static readonly GameEngine Engine = new(Modes);

    public static readonly Snapshots Snapshots = new(Modes);

    public const string PackId = "quiz";

    /// <summary>The image of <see cref="IllustratedQuestion"/>, named after its answer as an author would.</summary>
    public static readonly MediaPath Flag = new("images/drapeau-japon.png");

    /// <summary>A question whose correct answer is the first of its four choices, as authors often write them.</summary>
    public static QuizQuestion CapitalQuestion { get; } =
        Question("Quelle est la capitale de l’Australie ?", ("Canberra", true), ("Sydney", false), ("Melbourne", false), ("Perth", false));

    /// <summary>A question illustrated by <see cref="Flag"/>.</summary>
    public static QuizQuestion IllustratedQuestion { get; } =
        Question("De quel pays est ce drapeau ?", ("Japon", true), ("Bangladesh", false)) with { Image = Flag };

    /// <summary>A question that comes after the others: no screen but the console may show it before its turn.</summary>
    public static QuizQuestion LastQuestion { get; } =
        Question("Combien de joueurs compte une équipe de rugby à XV ?", ("Quinze", true), ("Treize", false), ("Onze", false));

    public static QuizRoundDescriptor Round(params QuizQuestion[] questions) =>
        new() { Title = "Culture générale", Questions = [.. questions] };

    public static QuizQuestion Question(string text, params (string Text, bool Correct)[] choices) => new()
    {
        Text = text,
        Choices = [.. choices.Select(choice => new QuizChoice { Text = choice.Text, Correct = choice.Correct })],
    };

    /// <summary>
    /// The same question, with its correct answer moved to another choice: the texts and their order stay the same.
    /// </summary>
    public static QuizQuestion WithCorrectChoice(QuizQuestion question, int correct) =>
        question with { Choices = [.. question.Choices.Select((choice, i) => choice with { Correct = i == correct })] };

    /// <summary>
    /// A game of a pack of the given quiz rounds, with the given players, whose first round has just started.
    /// </summary>
    /// <param name="rounds">The rounds of the pack.</param>
    /// <param name="nicknames">The players, registered in order before the start.</param>
    /// <param name="seed">The seed of the context the game starts in, which draws the shuffles.</param>
    public static GameState Started(ImmutableArray<QuizRoundDescriptor> rounds, string[] nicknames, int seed = 42)
    {
        ImmutableArray<RoundDescriptor> descriptors = [.. rounds];
        var pack = new CatalogPack(PackId, "Soirée quiz", rounds.Length, new PackDescriptor { FormatVersion = 1, Title = "Soirée quiz", Rounds = descriptors }, [])
        {
            Media = [.. rounds.SelectMany(r => r.Questions).Select(q => q.Image).OfType<MediaPath>().Distinct()],
        };
        var lobby = Games.Accepted(Games.Accepted(Games.LobbyWith(nicknames), Games.Loaded(pack)), Games.Select(PackId));
        return Accepted(lobby, Games.Start(), seed);
    }

    /// <summary>
    /// The state after an input the test expects the quiz mode to accept.
    /// </summary>
    public static GameState Accepted(GameState state, GameInput input, int seed = 42)
    {
        var transition = Engine.Handle(state, input, Games.Context(seed));
        Assert.Null(transition.Rejection);
        return transition.State;
    }

    /// <summary>
    /// The same game, its round in progress on another question, presented with its choices in the order of the
    /// descriptor, as if the questions before it were played.
    /// </summary>
    public static GameState AtQuestion(GameState state, int questionIndex)
    {
        var round = RoundOf(state);
        var order = Enumerable.Range(0, round.Descriptor.Questions[questionIndex].Choices.Length);
        var moved = new QuizRound(round.Descriptor, questionIndex, QuizPhase.Presentation, [.. order]);
        return state with { CurrentRound = state.CurrentRound! with { State = moved } };
    }

    /// <summary>The game master opens the answers of the question in progress.</summary>
    public static GameMasterRoundInput OpenAnswers(GameState state, int? questionNumber = null) =>
        new(new QuizOpenAnswers(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master reveals the answer of the question in progress.</summary>
    public static GameMasterRoundInput RevealAnswer(GameState state, int? questionNumber = null) =>
        new(new QuizRevealAnswer(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master moves on from the question in progress, once revealed.</summary>
    public static GameMasterRoundInput NextQuestion(GameState state, int? questionNumber = null) =>
        new(new QuizNextQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master skips the question in progress.</summary>
    public static GameMasterRoundInput SkipQuestion(GameState state, int? questionNumber = null) =>
        new(new QuizSkipQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>A player answers the question in progress, received by the hub at <paramref name="receivedAt"/>.</summary>
    public static PlayerRoundInput Answer(
        GameState state,
        int player,
        QuizChoiceLetter choice,
        DateTimeOffset? receivedAt = null,
        int? questionNumber = null) =>
        new(
            Games.PlayerIdOf(player),
            Games.NextClientSeq(state, player),
            new QuizSubmitAnswer(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber, choice),
            receivedAt ?? Games.Now);

    /// <summary>The timer that locks the answers of the question in progress elapses, when it was due.</summary>
    public static TimerElapsed AnswersTimerElapsed(GameState state, DateTimeOffset? dueAt = null) =>
        new(QuizMode.AnswersTimer, dueAt ?? RoundOf(state).AnswersCloseAt!.Value) { RoundId = state.CurrentRound!.Id };

    /// <summary>
    /// The same game, the answers of its question in progress opened, then answered by the given players, in this order.
    /// </summary>
    public static GameState Answering(GameState state, params (int Player, QuizChoiceLetter Choice)[] answers)
    {
        state = Accepted(state, OpenAnswers(state));
        foreach (var (player, choice) in answers)
        {
            state = Accepted(state, Answer(state, player, choice));
        }

        return state;
    }

    /// <summary>
    /// The same game, the answers of its question in progress opened, answered by the given players, then locked.
    /// </summary>
    public static GameState Locked(GameState state, params (int Player, QuizChoiceLetter Choice)[] answers) =>
        Closed(Answering(state, answers));

    /// <summary>
    /// The same game, the answers of its question in progress locked: already, once every participant answered, or else
    /// by the end of their countdown.
    /// </summary>
    public static GameState Closed(GameState state) =>
        RoundOf(state).Phase == QuizPhase.Answering ? Accepted(state, AnswersTimerElapsed(state)) : state;

    /// <summary>
    /// The same game, the answers of its question in progress opened, answered by the given players, locked, then
    /// revealed.
    /// </summary>
    public static GameState Revealed(GameState state, params (int Player, QuizChoiceLetter Choice)[] answers)
    {
        state = Locked(state, answers);
        return Accepted(state, RevealAnswer(state));
    }

    /// <summary>
    /// The round in progress of a game of quiz rounds.
    /// </summary>
    public static QuizRound RoundOf(GameState state) => Assert.IsType<QuizRound>(state.CurrentRound?.State);

    /// <summary>
    /// What each viewer receives for a state, serialized as the hub does, the phones named by the nickname of their player.
    /// </summary>
    public static ProjectionSet Projected(GameState state) =>
        new(
            Snapshots.ForDisplay(state),
            Snapshots.ForGameMaster(state),
            [.. state.Players.Select(p => (p.Nickname, (object)Snapshots.ForPlayer(state, p)))],
            ContractJsonOptions.Default);
}
