using PartyGame.Contracts;
using PartyGame.Engine.Effects;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Decision of the game master about the game a restarted server found saved: resume it, or start a new game in the lobby.
/// Each decision names the game found, so that a second one, from a double tap or another console, changes nothing.
/// </summary>
internal static class SavedGameChoice
{
    public static Transition Resume(GameState state, ResumeSavedGame resume)
    {
        if (PendingOf(state, resume.SavedGameId) is not { } pending)
        {
            return Transition.Rejected(state, RejectionReason.SavedGameObsolete);
        }

        if (!pending.MissingMedia.IsEmpty)
        {
            // A resumed game must never fail because of its content.
            return Transition.Rejected(state, RejectionReason.SavedGameMediaMissing);
        }

        // The addresses come from this startup: those of the game saved may belong to a network the host has left.
        return new Transition(
            pending.Game with { JoinAddress = state.JoinAddress, JoinAddressCandidates = state.JoinAddressCandidates },
            []);
    }

    public static Transition Discard(GameState state, DiscardSavedGame discard) =>
        PendingOf(state, discard.SavedGameId) is null
            ? Transition.Rejected(state, RejectionReason.SavedGameObsolete)
            : new Transition(state with { Phase = GamePhase.Lobby, PendingGame = null }, [new ArchiveSavedGame()]);

    public static Transition MediaChecked(GameState state, SavedGameMediaChecked check)
    {
        if (PendingOf(state, check.SavedGameId) is not { } pending)
        {
            return Transition.Rejected(state, RejectionReason.SavedGameObsolete);
        }

        if (pending.MissingMedia.SequenceEqual(check.MissingMedia))
        {
            // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
            return new Transition(state, []);
        }

        return new Transition(state with { PendingGame = pending with { MissingMedia = check.MissingMedia } }, []);
    }

    private static PendingGame? PendingOf(GameState state, GameId savedGameId) =>
        state is { Phase: GamePhase.ResumePending, PendingGame: { } pending } && pending.Game.GameId == savedGameId ? pending : null;
}
