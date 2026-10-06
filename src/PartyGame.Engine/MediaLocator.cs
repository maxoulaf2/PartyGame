using PartyGame.Engine.Modes;
using PartyGame.Engine.Projections;

namespace PartyGame.Engine;

/// <summary>
/// Finds where the game shows a media file, from the identifier a client knows it by: the clients never see its path.
/// </summary>
/// <param name="modes">The game modes, which alone know which step of their rounds shows a file.</param>
public sealed class MediaLocator(GameModes modes)
{
    /// <summary>
    /// Finds where the game shows a media file.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="id">The identifier of the file, as received from a client.</param>
    /// <returns>
    /// The file and where it shows, or <see langword="null"/> when the game has no media file with this identifier.
    /// </returns>
    public MediaLocation? Locate(GameState state, MediaId id)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Preview is { } preview)
        {
            // The TV screen shows the media files of the step previewed only.
            return preview.Media.Find(id) is { } shown ? new MediaLocation(shown, preview.Round, preview.StepIndex + 1) : null;
        }

        if (state.Media.Find(id) is not { } media)
        {
            return null;
        }

        // Between two rounds, the last round played is over: what fails then is not part of it.
        if (state is not { Phase: GamePhase.Round, CurrentRound: { } round })
        {
            return new MediaLocation(media, Round: null, Step: null);
        }

        var step = modes.For(state.Rounds[round.Index]).LocateMedia(round.State, media);
        return new MediaLocation(media, Snapshots.RoundInfoOf(state), step);
    }
}
