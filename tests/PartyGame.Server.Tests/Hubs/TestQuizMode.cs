using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Plays quiz rounds for the hub tests, with the placeholder intents of the contracts until the quiz mode plays its own
/// (US-E08-03 to US-E08-05): it counts the intents of the players, and any intent of the game master finishes the round.
/// Its views are those of the quiz mode.
/// </summary>
internal sealed class TestQuizMode : GameMode<QuizRoundDescriptor, TestQuizRound>
{
    private static readonly QuizMode _quiz = new();

    public override ImmutableArray<PackProblem> Validate(QuizRoundDescriptor descriptor, string path) => [];

    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new TestQuizRound((QuizRound)_quiz.Start(descriptor, game, context).State, PlayerIntents: 0), []);

    public override RoundTransition Handle(TestQuizRound round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            PlayerRoundInput => new RoundTransition(round with { PlayerIntents = round.PlayerIntents + 1 }, []),
            GameMasterRoundInput => new RoundTransition(round, []) { IsFinished = true },
            _ => RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
        };

    public override PlayerRoundView ProjectForPlayer(TestQuizRound round, GameState game, Player player) =>
        _quiz.ProjectForPlayer(round.Quiz, game, player);

    public override DisplayRoundView ProjectForDisplay(TestQuizRound round, GameState game) => _quiz.ProjectForDisplay(round.Quiz, game);

    public override GameMasterRoundView ProjectForGameMaster(TestQuizRound round, GameState game) =>
        _quiz.ProjectForGameMaster(round.Quiz, game);
}
