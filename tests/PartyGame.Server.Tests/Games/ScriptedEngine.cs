using PartyGame.Contracts;
using PartyGame.Engine;
using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Tests.Games;

/// <summary>
/// An engine whose behavior each test chooses, by default one that registers a player per <see cref="TestInput"/>.
/// </summary>
internal sealed class ScriptedEngine(Func<GameState, GameInput, GameContext, Transition>? handle = null) : IGameEngine
{
    private int _running;

    /// <summary>Highest number of calls seen running at the same time.</summary>
    public int MaxConcurrentCalls { get; private set; }

    public GameContext? LastContext { get; private set; }

    public Transition Handle(GameState state, GameInput input, GameContext context)
    {
        var running = Interlocked.Increment(ref _running);
        MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, running);
        try
        {
            LastContext = context;
            return (handle ?? AddPlayer)(state, input, context);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }

    /// <summary>
    /// Registers a player named after the value of the input, so that the final state records the order of the inputs.
    /// </summary>
    public static Transition AddPlayer(GameState state, GameInput input, GameContext context)
    {
        var value = ((TestInput)input).Value;
        var player = new Player(new PlayerId(Guid.NewGuid()), value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new Transition(state with { Players = state.Players.Add(player) }, []);
    }
}
