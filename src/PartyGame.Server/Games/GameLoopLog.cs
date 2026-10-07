using PartyGame.Engine;
using PartyGame.Engine.State;

namespace PartyGame.Server.Games;

internal static partial class GameLoopLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Game {GameId} created")]
    public static partial void GameCreated(this ILogger logger, Guid gameId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game loop stopped")]
    public static partial void GameLoopStopped(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Input {InputType} rejected: {Rejection}")]
    public static partial void InputRejected(this ILogger logger, string inputType, RejectionReason rejection);

    [LoggerMessage(Level = LogLevel.Error, Message = "Input {InputType} failed in phase {Phase}, previous state kept")]
    public static partial void InputFailed(this ILogger logger, Exception exception, string inputType, GamePhase phase);

    [LoggerMessage(Level = LogLevel.Error, Message = "Effect {EffectType} failed")]
    public static partial void EffectFailed(this ILogger logger, Exception exception, string effectType);

    [LoggerMessage(Level = LogLevel.Error, Message = "State change listener {Listener} failed")]
    public static partial void ListenerFailed(this ILogger logger, Exception exception, string listener);

    [LoggerMessage(Level = LogLevel.Information, Message = "Round {RoundNumber} of {RoundCount} started: {RoundTitle} ({Descriptor})")]
    public static partial void RoundStarted(this ILogger logger, int roundNumber, int roundCount, string roundTitle, string descriptor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Round {RoundNumber} of {RoundCount} finished")]
    public static partial void RoundFinished(this ILogger logger, int roundNumber, int roundCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Round {RoundNumber} of {RoundCount} skipped by the game master")]
    public static partial void RoundSkipped(this ILogger logger, int roundNumber, int roundCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Game finished")]
    public static partial void GameFinished(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Timer {TimerId} elapsed but its input could not be enqueued")]
    public static partial void TimerInputLost(this ILogger logger, Exception exception, string timerId);
}
