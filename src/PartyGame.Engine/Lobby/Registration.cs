using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Registration of players. It stays open in every phase: phones may join after the game started, and even once it is
/// finished, to see its end.
/// </summary>
internal static class Registration
{
    public static Transition Join(GameState state, JoinGame join)
    {
        // The hub generates these values, so a known one means the same registration handled twice. A code drawn twice is
        // unlikely, but it would designate two players: refused all the same.
        if (state.Players.Any(p => p.Id == join.PlayerId)
            || state.PlayerTokens.ContainsKey(join.Token)
            || state.ReconnectionCodes.ContainsValue(join.ReconnectionCode))
        {
            return Transition.Rejected(state, RejectionReason.PlayerAlreadyJoined);
        }

        if (!NicknameRules.TryNormalize(join.Nickname, out var nickname))
        {
            return Transition.Rejected(state, RejectionReason.NicknameInvalid);
        }

        if (state.Players.Any(p => NicknameRules.AreSame(p.Nickname, nickname)))
        {
            return Transition.Rejected(state, RejectionReason.NicknameTaken);
        }

        var newState = state with
        {
            Players = state.Players.Add(
                new Player(join.PlayerId, nickname, IsConnected: true, JoinedAfterEnd: state.Phase == GamePhase.Finished)),
            PlayerTokens = state.PlayerTokens.Add(join.Token, join.PlayerId),
            ReconnectionCodes = state.ReconnectionCodes.Add(join.PlayerId, join.ReconnectionCode),
        };
        return new Transition(newState, []);
    }
}
