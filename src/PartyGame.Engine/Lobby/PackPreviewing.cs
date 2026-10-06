using PartyGame.Engine.Audio;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Rounds;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Preview of a pack on the TV screen by the game master, in the lobby only: each step of its rounds, revealed, one at a
/// time. The phones stay on the lobby, and the start of the game ends it.
/// </summary>
internal static class PackPreviewing
{
    public static Transition Start(GameState state, StartPreview start, GameModes modes, GameContext context)
    {
        if (state.Phase != GamePhase.Lobby)
        {
            return Transition.Rejected(state, RejectionReason.GameAlreadyStarted);
        }

        if (state.Catalog.Find(start.PackId) is not { } pack)
        {
            return Transition.Rejected(state, RejectionReason.PackUnknown);
        }

        if (!pack.IsValid)
        {
            return Transition.Rejected(state, RejectionReason.PackInvalid);
        }

        if (pack.Descriptor.Rounds.Any(descriptor => !modes.TryFind(descriptor, out _)))
        {
            return Transition.Rejected(state, RejectionReason.GameModeMissing);
        }

        // Media identifiers of their own, as for a game: the TV screen loads the files as it would during the evening.
        var preview = new PackPreview(
            pack.Id,
            pack.Descriptor,
            PackMedia.Draw(pack.Media, context.Random),
            [.. pack.Descriptor.Rounds.Select(_ => RoundFlow.NewRoundId(context.Random))],
            RoundIndex: 0,
            StepIndex: 0,
            ExcerptStartsAt: null);
        return new Transition(state with { Preview = preview }, []);
    }

    public static Transition Show(GameState state, ShowPreviewStep show, GameModes modes, GameContext context)
    {
        if (state.Preview is not { } preview)
        {
            return Transition.Rejected(state, RejectionReason.PreviewNotStarted);
        }

        var roundIndex = show.RoundNumber - 1;
        var stepIndex = show.StepNumber - 1;
        if (roundIndex < 0
            || roundIndex >= preview.Pack.Rounds.Length
            || stepIndex < 0
            || stepIndex >= modes.For(preview.Pack.Rounds[roundIndex]).CountPreviewSteps(preview.Pack.Rounds[roundIndex]))
        {
            return Transition.Rejected(state, RejectionReason.PreviewStepUnknown);
        }

        // Played again from its start at each request, the time for the TV screen to set it, as during a game.
        var shown = preview with
        {
            RoundIndex = roundIndex,
            StepIndex = stepIndex,
            ExcerptStartsAt = show.PlayExcerpt ? context.Now + ExcerptPlayback.Lead : null,
        };

        // Accepted, but nothing changes: the same instance tells the loop that there is nothing to broadcast.
        return shown == preview ? new Transition(state, []) : new Transition(state with { Preview = shown }, []);
    }

    public static Transition Stop(GameState state) =>
        state.Preview is null
            ? Transition.Rejected(state, RejectionReason.PreviewNotStarted)
            : new Transition(state with { Preview = null }, []);
}
