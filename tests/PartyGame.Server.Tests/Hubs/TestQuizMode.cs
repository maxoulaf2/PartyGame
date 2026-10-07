using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Modes.Quiz;
using PartyGame.Engine.State;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// Plays quiz rounds for the hub tests that need rounds to end, which the quiz mode cannot do before US-E08-05: it counts
/// the intents of the players, each one worth <see cref="PointsPerIntent"/> to its player, and any intent of the game master
/// finishes the round. Its views are those of the quiz mode.
/// </summary>
internal sealed class TestQuizMode : GameMode<QuizRoundDescriptor, TestQuizRound>
{
    public const int PointsPerIntent = 100;

    private static readonly QuizMode _quiz = new();

    public override ImmutableArray<PackProblem> Validate(QuizRoundDescriptor descriptor, string path) => [];

    public override RoundTransition Start(QuizRoundDescriptor descriptor, GameState game, GameContext context) =>
        new(new TestQuizRound((QuizRound)_quiz.Start(descriptor, game, context).State, PlayerIntents: 0), []);

    public override RoundTransition Handle(TestQuizRound round, GameInput input, GameState game, GameContext context) =>
        input switch
        {
            PlayerRoundInput player => new RoundTransition(round with { PlayerIntents = round.PlayerIntents + 1 }, [])
            {
                Points = ImmutableDictionary<PlayerId, int>.Empty.Add(player.PlayerId, PointsPerIntent),
            },
            GameMasterRoundInput => new RoundTransition(round, []) { IsFinished = true },
            _ => RoundTransition.Rejected(round, RejectionReason.UnexpectedTimer),
        };

    public override RoundTransition ResumeRound(TestQuizRound round, GameState game, TimeSpan shift, GameContext context) =>
        new(round, []);

    public override PlayerRoundView ProjectForPlayer(TestQuizRound round, GameState game, Player player) =>
        _quiz.ProjectForPlayer(round.Quiz, game, player);

    public override DisplayRoundView ProjectForDisplay(TestQuizRound round, GameState game) => _quiz.ProjectForDisplay(round.Quiz, game);

    public override GameMasterRoundView ProjectForGameMaster(TestQuizRound round, GameState game) =>
        _quiz.ProjectForGameMaster(round.Quiz, game);

    public override int CountPreviewSteps(QuizRoundDescriptor descriptor) => _quiz.CountPreviewSteps(descriptor);

    public override RoundPreview Preview(QuizRoundDescriptor descriptor, int stepIndex, DateTimeOffset? excerptStartsAt) =>
        new(new TestQuizRound((QuizRound)_quiz.Preview(descriptor, stepIndex, excerptStartsAt).Round, PlayerIntents: 0), HasExcerpt: false);
}
