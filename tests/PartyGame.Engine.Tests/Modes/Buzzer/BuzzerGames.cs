using System.Collections.Immutable;
using PartyGame.Contracts.Buzzer;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Serialization;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Buzzer;
using PartyGame.Engine.Packs;
using PartyGame.Engine.Projections;
using PartyGame.Engine.State;
using PartyGame.Tests.Shared.Leaks;
using EngineBuzzer = PartyGame.Engine.Modes.Common.Buzzer;

namespace PartyGame.Engine.Tests.Modes.Buzzer;

/// <summary>
/// Builds the rounds of buzzer questions and the games that play them, with the real buzzer mode.
/// </summary>
internal static class BuzzerGames
{
    public static readonly BuzzerMode Mode = new();

    public static readonly GameModes Modes = new([Mode]);

    public static readonly GameEngine Engine = new(Modes);

    public static readonly Snapshots Snapshots = new(Modes);

    public const string PackId = "buzzer";

    /// <summary>The image of <see cref="IllustratedQuestion"/>, named after its answer as an author would.</summary>
    public static readonly MediaPath Flag = new("images/drapeau-italie.png");

    public static BuzzerQuestion PaintingQuestion { get; } = new() { Text = "Qui a peint La Joconde ?", Answer = "Léonard de Vinci" };

    public static BuzzerQuestion IllustratedQuestion { get; } =
        new() { Text = "De quel pays est ce drapeau ?", Answer = "L’Italie", Image = Flag };

    /// <summary>A question that comes after the others: no screen but the console may show it before its turn.</summary>
    public static BuzzerQuestion LastQuestion { get; } = new() { Text = "Combien de joueurs compte une équipe de rugby à XV ?", Answer = "Quinze" };

    public static BuzzerRoundDescriptor Round(params BuzzerQuestion[] questions) =>
        new() { Title = "Le plus rapide", Questions = [.. questions] };

    /// <summary>
    /// A game of a pack of the given round, with the given players, registered in order, whose round has just started.
    /// </summary>
    public static GameState Started(BuzzerRoundDescriptor round, params string[] nicknames)
    {
        ImmutableArray<RoundDescriptor> descriptors = [round];
        var pack = new CatalogPack(PackId, "Soirée buzzer", 1, new PackDescriptor { FormatVersion = 1, Title = "Soirée buzzer", Rounds = descriptors }, [])
        {
            Media = [.. round.Questions.Select(q => q.Image).OfType<MediaPath>().Distinct()],
        };
        var lobby = Games.Accepted(Games.Accepted(Games.LobbyWith(nicknames), Games.Loaded(pack)), Games.Select(PackId));
        var announced = Accepted(lobby, Games.Start());
        return Accepted(announced, Games.StartRound(announced));
    }

    /// <summary>
    /// The state after an input the test expects the buzzer mode to accept.
    /// </summary>
    public static GameState Accepted(GameState state, GameInput input, GameContext? context = null)
    {
        var transition = Engine.Handle(state, input, context ?? Games.Context());
        Assert.Null(transition.Rejection);
        return transition.State;
    }

    /// <summary>
    /// The same game, its round in progress on another question, as if the questions before it were played.
    /// </summary>
    public static GameState AtQuestion(GameState state, int questionIndex) =>
        state with { CurrentRound = state.CurrentRound! with { State = new BuzzerRound(RoundOf(state).Descriptor, questionIndex) } };

    /// <summary>The game master asks the question in progress, shown on the TV screen unless told otherwise.</summary>
    public static GameMasterRoundInput AskQuestion(GameState state, int? questionNumber = null, bool show = true) =>
        new(new BuzzerAskQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber, show), Games.Now);

    /// <summary>The game master shows the question in progress on the TV screen.</summary>
    public static GameMasterRoundInput ShowQuestion(GameState state, int? questionNumber = null) =>
        new(new BuzzerShowQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master judges the answer of the player who has the hand on the current opening.</summary>
    public static GameMasterRoundInput Judge(GameState state, bool correct, int? opening = null, int? questionNumber = null) =>
        new(
            new BuzzerJudge(
                state.CurrentRound!.Id,
                questionNumber ?? RoundOf(state).QuestionNumber,
                opening ?? RoundOf(state).Buzzer.Opening,
                correct),
            Games.Now);

    /// <summary>The game master reveals the answer of the question in progress.</summary>
    public static GameMasterRoundInput RevealAnswer(GameState state, int? questionNumber = null) =>
        new(new BuzzerRevealAnswer(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>The game master moves on from the question in progress.</summary>
    public static GameMasterRoundInput NextQuestion(GameState state, int? questionNumber = null) =>
        new(new BuzzerNextQuestion(state.CurrentRound!.Id, questionNumber ?? RoundOf(state).QuestionNumber), Games.Now);

    /// <summary>
    /// A player buzzes on the current opening of the question in progress, pressed at the given time, by default at
    /// <see cref="Games.Now"/>, and received at once unless told otherwise.
    /// </summary>
    public static PlayerRoundInput Buzz(
        GameState state,
        int player,
        DateTimeOffset? pressedAt = null,
        DateTimeOffset? receivedAt = null,
        int? questionNumber = null,
        int? opening = null) =>
        new(
            Games.PlayerIdOf(player),
            Games.NextClientSeq(state, player),
            new BuzzerBuzz(
                state.CurrentRound!.Id,
                questionNumber ?? RoundOf(state).QuestionNumber,
                opening ?? RoundOf(state).Buzzer.Opening,
                (pressedAt ?? Games.Now).ToUnixTimeMilliseconds()),
            receivedAt ?? pressedAt ?? Games.Now);

    /// <summary>The arbitration window of the buzzer in progress ends.</summary>
    public static TimerElapsed ArbitrationElapsed(GameState state) =>
        new(EngineBuzzer.ArbitrationTimer, RoundOf(state).Buzzer.ArbitrateAt!.Value) { RoundId = state.CurrentRound!.Id };

    /// <summary>The same game, its question in progress asked: the buzzer is open.</summary>
    public static GameState Asked(GameState state) => Accepted(state, AskQuestion(state));

    /// <summary>The same game, its buzzer open while the TV screen does not show the question yet.</summary>
    public static GameState AskedHidden(GameState state) => Accepted(state, AskQuestion(state, show: false));

    /// <summary>
    /// The same game, its question asked, then buzzed by the given players in this order, each pressed the given number of
    /// milliseconds after the opening: the arbitration window runs.
    /// </summary>
    public static GameState Buzzed(GameState state, params (int Player, int PressedAfter)[] buzzes)
    {
        state = Asked(state);
        foreach (var (player, pressedAfter) in buzzes)
        {
            state = Accepted(state, Buzz(state, player, Games.Now.AddMilliseconds(pressedAfter)));
        }

        return state;
    }

    /// <summary>
    /// The same game, buzzed as <see cref="Buzzed"/> does, then arbitrated: the earliest press has the hand.
    /// </summary>
    public static GameState Answering(GameState state, params (int Player, int PressedAfter)[] buzzes)
    {
        state = Buzzed(state, buzzes);
        return Accepted(state, ArbitrationElapsed(state));
    }

    /// <summary>
    /// The same game, its question asked, then buzzed by each given player in turn, alone, whose answer is judged:
    /// <paramref name="correct"/> for the last one, wrong for the others.
    /// </summary>
    public static GameState Judged(GameState state, bool correct, params int[] players)
    {
        state = Asked(state);
        for (var index = 0; index < players.Length; index++)
        {
            state = Accepted(state, Buzz(state, players[index]));
            state = Accepted(state, ArbitrationElapsed(state));
            state = Accepted(state, Judge(state, correct && index == players.Length - 1));
        }

        return state;
    }

    /// <summary>
    /// The same game, the given players blocked on the buzzer of the question in progress, as after their wrong answers.
    /// </summary>
    public static GameState WithBlocked(GameState state, params int[] players)
    {
        var round = RoundOf(state);
        var buzzer = players.Aggregate(round.Buzzer, (blocked, player) => blocked.Block(Games.PlayerIdOf(player)));
        return state with { CurrentRound = state.CurrentRound! with { State = round with { Buzzer = buzzer } } };
    }

    /// <summary>
    /// The same game, its question in progress kept off the TV screen, as when the game master asks it without showing it.
    /// </summary>
    public static GameState Hidden(GameState state) =>
        state with { CurrentRound = state.CurrentRound! with { State = RoundOf(state) with { Shown = false } } };

    /// <summary>
    /// The round in progress of a game of buzzer questions.
    /// </summary>
    public static BuzzerRound RoundOf(GameState state) => Assert.IsType<BuzzerRound>(state.CurrentRound?.State);

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
