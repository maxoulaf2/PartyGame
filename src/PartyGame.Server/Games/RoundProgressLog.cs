using PartyGame.Contracts;
using PartyGame.Engine;

namespace PartyGame.Server.Games;

/// <summary>
/// Logs for the operator when a round starts, when it finishes or is skipped, and when the game is finished, whatever made it happen: an
/// intent, a timer, or the end decided by a game mode.
/// </summary>
/// <remarks>Called by the loop only, one change at a time: the last state seen needs no lock.</remarks>
internal sealed class RoundProgressLog(ILogger<RoundProgressLog> logger) : IGameStateListener
{
    private GamePhase _phase = GamePhase.Lobby;
    private RoundId? _roundId;

    public ValueTask OnStateChangedAsync(GameState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        // A round announced is not started yet: it is logged once its game mode plays it.
        if (state.Phase == GamePhase.RoundIntro)
        {
            _phase = state.Phase;
            return ValueTask.CompletedTask;
        }

        if (state.CurrentRound is { } round)
        {
            var number = round.Index + 1;
            if (round.Id != _roundId)
            {
                var descriptor = state.Rounds[round.Index];
                logger.RoundStarted(number, state.Rounds.Length, descriptor.Title, descriptor.GetType().Name);
            }

            if (state.Phase != GamePhase.Round && (round.Id != _roundId || _phase == GamePhase.Round))
            {
                if (round.IsSkipped)
                {
                    logger.RoundSkipped(number, state.Rounds.Length);
                }
                else
                {
                    logger.RoundFinished(number, state.Rounds.Length);
                }
            }

            _roundId = round.Id;
        }

        if (state.Phase == GamePhase.Finished && _phase != GamePhase.Finished)
        {
            logger.GameFinished();
        }

        _phase = state.Phase;
        return ValueTask.CompletedTask;
    }
}
