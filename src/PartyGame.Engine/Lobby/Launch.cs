using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Packs;
using PartyGame.Engine.Rounds;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Start of the game by the game master, once: the pack chosen is fixed, and its first round is announced. Registration
/// stays open afterwards.
/// </summary>
internal static class Launch
{
    /// <summary>
    /// Registered players, connected or not, below which the game cannot start. One player eases the tests; a higher
    /// value may become configurable.
    /// </summary>
    public const int MinimumPlayerCount = 1;

    public static Transition Start(GameState state, StartGame start, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        if (state.Players.Length < MinimumPlayerCount)
        {
            return Transition.Rejected(state, RejectionReason.NotEnoughPlayers);
        }

        // Only a valid pack can be selected, and a reload cancels the selection of a pack that is no longer valid.
        if (state.SelectedPackId is not { } packId || state.Catalog.Find(packId) is not { IsValid: true } pack)
        {
            return Transition.Rejected(state, RejectionReason.PackNotSelected);
        }

        // Copied into the game, which then depends neither on the catalog nor on the disk: it is persisted with it (E11).
        // The identifiers of the media files are drawn now, once for the whole game, so that a URL always serves the same
        // file and a client may cache it.
        var started = state with
        {
            Pack = pack.Descriptor,
            Media = PackMedia.Draw(pack.Media, context.Random),
            Preview = null,
            Schedule = RoundSchedule.Empty with { Upcoming = [.. Enumerable.Range(0, pack.Descriptor.Rounds.Length)] },
        };

        // Checked once and for all here, so that no round can fail to start in the middle of the game.
        if (started.Rounds.Any(descriptor => !modes.TryFind(descriptor, out _)))
        {
            return Transition.Rejected(state, RejectionReason.GameModeMissing);
        }

        return RoundFlow.Announce(started, context);
    }
}
