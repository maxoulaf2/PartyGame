using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Whether each player's phone is connected, which the TV screen and the game master show.
/// </summary>
internal static class Presence
{
    public static Transition ConnectionLost(GameState state, PlayerConnectionLost lost)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == lost.PlayerId);
        if (player is null)
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        if (!player.IsConnected)
        {
            return Transition.Rejected(state, RejectionReason.PlayerAlreadyDisconnected);
        }

        var newState = state with { Players = state.Players.Replace(player, player with { IsConnected = false }) };
        return new Transition(newState, []);
    }
}
