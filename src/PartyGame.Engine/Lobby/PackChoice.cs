using PartyGame.Engine.Inputs;
using PartyGame.Engine.Packs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Choice of the pack by the game master, and reload of the packs, in the lobby only: once the game is started, its pack is
/// fixed, and nothing that happens to the files can reach it.
/// </summary>
internal static class PackChoice
{
    public static Transition Select(GameState state, SelectPack select)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        if (state.Catalog.Find(select.PackId) is not { } pack)
        {
            return Transition.Rejected(state, RejectionReason.PackUnknown);
        }

        if (!pack.IsValid)
        {
            return Transition.Rejected(state, RejectionReason.PackInvalid);
        }

        if (string.Equals(state.SelectedPackId, pack.Id, StringComparison.Ordinal))
        {
            return Transition.Unchanged(state);
        }

        return new Transition(state with { SelectedPackId = pack.Id }, []);
    }

    public static Transition Load(GameState state, PacksLoaded loaded)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        return new Transition(WithCatalog(state, loaded.Catalog), []);
    }

    /// <summary>
    /// Replaces the catalog. The selection is kept while its pack is still there and valid, and cancelled otherwise. With
    /// no selection, a single valid pack is chosen at once: the game master has nothing to choose.
    /// </summary>
    /// <remarks>
    /// A cancelled selection is never replaced by another pack, even a single valid one: the game would not play the pack
    /// the game master chose without them noticing.
    /// </remarks>
    public static GameState WithCatalog(GameState state, PackCatalog catalog)
    {
        string? selected;
        if (state.SelectedPackId is { } previous)
        {
            selected = catalog.Find(previous) is { IsValid: true } ? previous : null;
        }
        else
        {
            var valid = catalog.Packs.Where(pack => pack.IsValid).Take(2).ToList();
            selected = valid is [var single] ? single.Id : null;
        }

        return state with { Catalog = catalog, SelectedPackId = selected };
    }
}
