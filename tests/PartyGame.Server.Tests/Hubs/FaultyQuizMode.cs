using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Plays quiz rounds with injected bugs: every intent of a player throws, any intent of the game master finishes the round,
/// and the projection of <paramref name="failingRole"/>, if any, throws. Its views are those of the quiz mode.
/// </summary>
internal sealed class FaultyQuizMode(Role? failingRole = null) : GameMode<QuizRoundDescriptor, TestQuizRound>
{
    private static readonly QuizMode _quiz = new();

    public override ImmutableArray<PackProblem> Validate(QuizRoundDescriptor descriptor, string path) => [];

    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new TestQuizRound((QuizRound)_quiz.Start(descriptor, game, context).State, PlayerIntents: 0), []);

    public override RoundTransition Handle(TestQuizRound round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            PlayerRoundInput => throw new InvalidOperationException("Injected mode failure"),
            GameMasterRoundInput => new RoundTransition(round, []) { IsFinished = true },
            _ => RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
        };

    public override PlayerRoundView ProjectForPlayer(TestQuizRound round, GameState game, Player player)
    {
        ThrowIfFailing(Role.Player);
        return _quiz.ProjectForPlayer(round.Quiz, game, player);
    }

    public override DisplayRoundView ProjectForDisplay(TestQuizRound round, GameState game)
    {
        ThrowIfFailing(Role.Display);
        return _quiz.ProjectForDisplay(round.Quiz, game);
    }

    public override GameMasterRoundView ProjectForGameMaster(TestQuizRound round, GameState game)
    {
        ThrowIfFailing(Role.GameMaster);
        return _quiz.ProjectForGameMaster(round.Quiz, game);
    }

    private void ThrowIfFailing(Role role)
    {
        if (role == failingRole)
        {
            throw new InvalidOperationException($"Injected {role} projection failure");
        }
    }
}
