using PartyGame.Contracts;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Whether each player's phone is connected, which the TV screen and the game master show.
/// </summary>
internal static class Presence
{
    public static Transition ConnectionLost(GameState state, PlayerConnectionLost lost) =>
        SetConnected(state, lost.PlayerId, isConnected: false, RejectionReason.PlayerAlreadyDisconnected);

    public static Transition ConnectionRestored(GameState state, PlayerConnectionRestored restored) =>
        SetConnected(state, restored.PlayerId, isConnected: true, RejectionReason.PlayerAlreadyConnected);

    private static Transition SetConnected(GameState state, PlayerId playerId, bool isConnected, RejectionReason unchanged)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        if (player.IsConnected == isConnected)
        {
            return Transition.Rejected(state, unchanged);
        }

        var newState = state with { Players = state.Players.Replace(player, player with { IsConnected = isConnected }) };
        return new Transition(newState, []);
    }
}
