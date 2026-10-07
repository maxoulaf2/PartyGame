using System.Collections.Immutable;
using PartyGame.Contracts.OpenQuestion;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.OpenQuestion;
using PartyGame.Engine.Packs;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Modes.OpenQuestion;

/// <summary>
/// Builds the rounds of open questions and the games that play them, with the real mode.
/// </summary>
internal static class OpenQuestionGames
{
    public static readonly OpenQuestionMode Mode = new();

    public static readonly GameModes Modes = new([Mode]);

    public static readonly GameEngine Engine = new(Modes);

    public static readonly Snapshots Snapshots = new(Modes);

    public const string PackId = "openquestion";

    /// <summary>The image of <see cref="FlagQuestion"/>, named after its answer as an author would.</summary>
    public static readonly MediaPath Flag = new("images/drapeau-italie.png");

    public static OpenQuestionDescriptor PaintingQuestion { get; } = new()
    {
        Text = "Qui a peint La Joconde ?",
        Answer = "Léonard de Vinci",
        AcceptedAnswers = ["De Vinci"],
    };

    public static OpenQuestionDescriptor FlagQuestion { get; } = new()
    {
        Text = "De quel pays est ce drapeau ?",
        Answer = "L'Italie",
        Image = Flag,
    };

    public static OpenQuestionDescriptor YearQuestion { get; } = new()
    {
        Text = "En quelle année l'homme a-t-il marché sur la Lune ?",
        Answer = "1969",
        InputMode = OpenQuestionInputMode.Numeric,
        AnswerSeconds = 15,
    };

    public static OpenQuestionRoundDescriptor Round(params OpenQuestionDescriptor[] questions) =>
        new() { Title = "Réponses libres", MaxLength = 20, Questions = [.. questions] };

    /// <summary>
    /// A game of a pack of the given rounds, with the given players, whose first round has just started.
    /// </summary>
    public static GameState Started(ImmutableArray<OpenQuestionRoundDescriptor> rounds, string[] nicknames)
    {
        ImmutableArray<RoundDescriptor> descriptors = [.. rounds];
        var pack = new CatalogPack(PackId, "Soirée libre", rounds.Length, new PackDescriptor { FormatVersion = 1, Title = "Soirée libre", Rounds = descriptors }, [])
        {
            Media = [.. rounds.SelectMany(r => r.Questions).Select(q => q.Image).OfType<MediaPath>().Distinct()],
        };
        var lobby = Games.Accepted(Games.Accepted(Games.LobbyWith(nicknames), Games.Loaded(pack)), Games.Select(PackId));
        var announced = Accepted(lobby, Games.Start());
        return Accepted(announced, Games.StartRound(announced));
    }

    /// <summary>
    /// The state after an input the test expects the mode to accept.
    /// </summary>
    public static GameState Accepted(GameState state, GameInput input)
    {
        var transition = Engine.Handle(state, input, Games.Context());
        Assert.Null(transition.Rejection);
        return transition.State;
    }

    /// <summary>The game master shows the question in progress on the TV screen.</summary>
    public static GameMasterRoundInput ShowQuestion(GameState state, int? questionNumber = null) =>
        new(new OpenQuestionShowQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master skips the question in progress.</summary>
    public static GameMasterRoundInput SkipQuestion(GameState state, int? questionNumber = null) =>
        new(new OpenQuestionSkipQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>A player answers the question in progress, received by the hub at <paramref name="receivedAt"/>.</summary>
    public static PlayerRoundInput Answer(GameState state, int player, string answer, DateTimeOffset? receivedAt = null, int? questionNumber = null) =>
        new(
            Games.PlayerIdOf(player),
            Games.NextClientSeq(state, player),
            new OpenQuestionSubmitAnswer(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber, answer),
            receivedAt ?? Games.Now);

    /// <summary>The timer that locks the answers of the question in progress elapses, when it was due.</summary>
    public static TimerElapsed AnswersTimerElapsed(GameState state, DateTimeOffset? dueAt = null) =>
        new(OpenQuestionMode.AnswersTimer, dueAt ?? RoundOf(state).AnswersCloseAt!.Value) { RoundId = state.CurrentRound!.Id };

    /// <summary>
    /// The same game, its question in progress shown, which opens the answers, then answered by the given players, in
    /// this order.
    /// </summary>
    public static GameState Answering(GameState state, params (int Player, string Answer)[] answers)
    {
        state = Accepted(state, ShowQuestion(state));
        foreach (var (player, answer) in answers)
        {
            state = Accepted(state, Answer(state, player, answer));
        }

        return state;
    }

    /// <summary>
    /// The same game, its question in progress shown, answered by the given players, then locked: already, once every
    /// participant answered, or else by the end of the countdown.
    /// </summary>
    public static GameState Locked(GameState state, params (int Player, string Answer)[] answers)
    {
        state = Answering(state, answers);
        return RoundOf(state).Phase == OpenQuestionPhase.Answering ? Accepted(state, AnswersTimerElapsed(state)) : state;
    }

    /// <summary>The game master judges the answers to the question in progress, accepting those of the given players.</summary>
    public static GameMasterRoundInput Judge(GameState state, int[] accepted, int? questionNumber = null) =>
        new(new OpenQuestionJudge(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber, [.. accepted.Select(Games.PlayerIdOf)]), Games.Now);

    /// <summary>
    /// The same game, its question in progress shown, answered by the given players, locked, then judged, accepting the
    /// answers of the players in <paramref name="accepted"/>.
    /// </summary>
    public static GameState Judged(GameState state, int[] accepted, params (int Player, string Answer)[] answers)
    {
        state = Locked(state, answers);
        return Accepted(state, Judge(state, accepted));
    }

    /// <summary>The game master reveals the question in progress.</summary>
    public static GameMasterRoundInput RevealAnswer(GameState state, int? questionNumber = null) =>
        new(new OpenQuestionRevealAnswer(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master moves on from the question revealed.</summary>
    public static GameMasterRoundInput NextQuestion(GameState state, int? questionNumber = null) =>
        new(new OpenQuestionNextQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>
    /// The same game, its question in progress answered by the given players, judged accepting the answers of the players
    /// in <paramref name="accepted"/>, unless nobody answered, then revealed.
    /// </summary>
    public static GameState Revealed(GameState state, int[] accepted, params (int Player, string Answer)[] answers)
    {
        state = Locked(state, answers);
        state = RoundOf(state).Phase == OpenQuestionPhase.Locked ? Accepted(state, Judge(state, accepted)) : state;
        return Accepted(state, RevealAnswer(state));
    }

    /// <summary>The same game, its question in progress skipped for the next one.</summary>
    public static GameState Skipped(GameState state) => Accepted(state, SkipQuestion(state));

    /// <summary>
    /// The round in progress of a game of open questions.
    /// </summary>
    public static OpenQuestionRound RoundOf(GameState state) => Assert.IsType<OpenQuestionRound>(state.CurrentRound?.State);

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
