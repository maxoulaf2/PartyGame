using PartyGame.Engine.Inputs;
using PartyGame.Engine.Lobby;

namespace PartyGame.Engine;

/// <summary>
/// The rules of the game, as a pure function: no I/O, no clock, no randomness of its own.
/// The game loop calls it for each input, one at a time.
/// </summary>
public sealed class GameEngine : IGameEngine
{
    /// <inheritdoc />
    public Transition Handle(GameState state, GameInput input, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        return input switch
        {
            JoinGame join => Registration.Join(state, join),
            PlayerConnectionLost lost => Presence.ConnectionLost(state, lost),
            RenamePlayer rename => Renaming.Rename(state, rename),
            StartGame start => Launch.Start(state, start),

            // No phase schedules a timer yet: any timer that elapses is obsolete.
            TimerElapsed => Transition.Rejected(state, RejectionReason.UnexpectedTimer),

            _ => throw new NotSupportedException($"Input {input.GetType().Name} is not handled by the engine."),
        };
    }
}
