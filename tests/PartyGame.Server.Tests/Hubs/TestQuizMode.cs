using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Contracts.Quiz;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Plays quiz rounds for the hub tests, with the placeholder types of the contracts until the quiz mode exists (E08): it
/// counts the intents of the players, and any intent of the game master finishes the round.
/// </summary>
internal sealed class TestQuizMode : GameMode<QuizRoundDescriptor, TestQuizRound>
{
    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new TestQuizRound(PlayerIntents: 0), []);

    public override RoundTransition Handle(TestQuizRound round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            PlayerRoundInput => new RoundTransition(round with { PlayerIntents = round.PlayerIntents + 1 }, []),
            GameMasterRoundInput => new RoundTransition(round, []) { IsFinished = true },
            _ => RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
        };

    public override PlayerRoundView ProjectForPlayer(TestQuizRound round, GameState game, Player player) => new QuizPlayerView();

    public override DisplayRoundView ProjectForDisplay(TestQuizRound round, GameState game) => new QuizDisplayView();

    public override GameMasterRoundView ProjectForGameMaster(TestQuizRound round, GameState game) => new QuizGameMasterView();
}
