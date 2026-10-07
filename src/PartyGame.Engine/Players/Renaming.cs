using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Players;

/// <summary>
/// Renaming of players by the game master, with the rules of the registration. Allowed in every phase and whether the
/// player is connected or not: a disconnected player finds the new nickname when they come back.
/// </summary>
internal static class Renaming
{
    public static Transition Rename(GameState state, RenamePlayer rename)
    {
        var player = state.Players.FirstOrDefault(p => p.Id == rename.PlayerId);
        if (player is null)
        {
            return Transition.Rejected(state, RejectionReason.PlayerUnknown);
        }

        if (!NicknameRules.TryNormalize(rename.Nickname, out var nickname))
        {
            return Transition.Rejected(state, RejectionReason.NicknameInvalid);
        }

        // The player's own nickname is no duplicate: changing its case or accents is a rename like any other.
        if (state.Players.Any(p => p.Id != player.Id && NicknameRules.AreSame(p.Nickname, nickname)))
        {
            return Transition.Rejected(state, RejectionReason.NicknameTaken);
        }

        if (string.Equals(player.Nickname, nickname, StringComparison.Ordinal))
        {
            return Transition.Unchanged(state);
        }

        var newState = state with { Players = state.Players.Replace(player, player with { Nickname = nickname }) };
        return new Transition(newState, []);
    }
}
