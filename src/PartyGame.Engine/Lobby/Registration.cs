using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Registration of players. It stays open in every phase: phones may join after the game started.
/// </summary>
internal static class Registration
{
    public static Transition Join(GameState state, JoinGame join)
    {
        // The hub generates both values, so a known one means the same registration handled twice.
        if (state.Players.Any(p => p.Id == join.PlayerId) || state.PlayerTokens.ContainsKey(join.Token))
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
            Players = state.Players.Add(new Player(join.PlayerId, nickname)),
            PlayerTokens = state.PlayerTokens.Add(join.Token, join.PlayerId),
        };
        return new Transition(newState, []);
    }
}
