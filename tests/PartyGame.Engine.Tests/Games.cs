using PartyGame.Contracts;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Tests;

/// <summary>
/// Builds the states, inputs and contexts that engine tests start from.
/// </summary>
internal static class Games
{
    public static readonly DateTimeOffset Now = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    public static readonly GameEngine Engine = new();

    public const string JoinAddress = "192.168.1.42";

    public static GameState NewLobby() => GameState.Create(new GameId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff")), JoinAddress);

    public static GameContext Context(int seed = 42) => new(Now, new Random(seed));

    public static JoinGame Join(string nickname, int player = 1) =>
        new(PlayerIdOf(player), new PlayerToken($"token-{player}"), nickname, Now);

    public static PlayerId PlayerIdOf(int player) => new(new Guid(player, 0, 0, new byte[8]));

    /// <summary>
    /// A lobby with the given players, registered in order.
    /// </summary>
    public static GameState LobbyWith(params string[] nicknames)
    {
        var state = NewLobby();
        for (var i = 0; i < nicknames.Length; i++)
        {
            state = Engine.Handle(state, Join(nicknames[i], player: i + 1), Context()).State;
        }

        return state;
    }
}
