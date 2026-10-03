using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine.Tests.Rounds;

/// <summary>
/// A game mode registered for the tests only. Its rounds record every input they get, schedule a countdown when they
/// start, and do what the intents of the game master tell them.
/// </summary>
internal sealed class FakeMode : GameMode<FakeRoundDescriptor, FakeRoundState>
{
    public static readonly TimerId Countdown = new("fake-countdown");

    public static readonly TimeSpan CountdownDuration = TimeSpan.FromSeconds(20);

    public const int AwardedPoints = 10;

    public override ImmutableArray<PackProblem> Validate(FakeRoundDescriptor descriptor, string path) => [];

    public override RoundTransition Start(FakeRoundDescriptor descriptor, GameState game, GameContext context)
    {
        var dueAt = context.Now + CountdownDuration;
        return new RoundTransition(
            new FakeRoundState(descriptor.Title, dueAt, [$"start with {game.Players.Length} players"]),
            [new ScheduleTimer(Countdown, dueAt)]);
    }

    public override RoundTransition Handle(FakeRoundState round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            PlayerRoundInput { RoundIntent: FakePlayerIntent { Action: FakePlayerIntent.Nothing } } =>
                new RoundTransition(round, []),
            PlayerRoundInput { RoundIntent: FakePlayerIntent { Action: FakePlayerIntent.Refused } } =>
                RoundTransition.Rejected(round, RejectionReason.PhaseMismatch),
            PlayerRoundInput { RoundIntent: FakePlayerIntent intent } player =>
                Record(round, $"player {player.PlayerId.Value} {intent.Action}"),
            GameMasterRoundInput { RoundIntent: FakeGameMasterIntent { Action: FakeGameMasterIntent.Finish } } =>
                new RoundTransition(round, []) { IsFinished = true },
            GameMasterRoundInput { RoundIntent: FakeGameMasterIntent { Action: FakeGameMasterIntent.Nothing } } =>
                new RoundTransition(round, []),
            GameMasterRoundInput { RoundIntent: FakeGameMasterIntent { Action: FakeGameMasterIntent.Award } } =>
                new RoundTransition(round, []) { Points = game.Players.ToImmutableDictionary(p => p.Id, _ => AwardedPoints) },
            GameMasterRoundInput { RoundIntent: FakeGameMasterIntent intent } => Record(round, $"game master {intent.Action}"),

            // A replaced countdown may still elapse: only the one the round waits for counts.
            TimerElapsed timer when timer.TimerId == Countdown && timer.DueAt != round.CountdownDueAt =>
                RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
            TimerElapsed timer => Record(round, $"timer {timer.TimerId.Value}"),

            _ => throw new NotSupportedException($"The fake mode does not handle {input.GetType().Name}."),
        };

    public override PlayerRoundView ProjectForPlayer(FakeRoundState round, GameState game, Player player) =>
        new FakePlayerView(player.Nickname, round.Inputs.Count);

    public override DisplayRoundView ProjectForDisplay(FakeRoundState round, GameState game) =>
        new FakeDisplayView(
            round.Title,
            round.Inputs.Count,
            game.Rounds[game.CurrentRound!.Index] is FakeRoundDescriptor { Image: { } image } ? game.Media.UrlOf(image) : null);

    public override GameMasterRoundView ProjectForGameMaster(FakeRoundState round, GameState game) =>
        new FakeGameMasterView(round.Title, [.. round.Inputs]);

    private static RoundTransition Record(FakeRoundState round, string input) =>
        new(round with { Inputs = round.Inputs.Add(input) }, []);
}
