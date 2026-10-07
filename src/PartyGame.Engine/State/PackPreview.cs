using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Packs;

namespace PartyGame.Engine.State;

/// <summary>
/// A pack the game master previews on the TV screen, in the lobby: one step of one of its rounds at a time, revealed. It is
/// no game: nobody plays it, and it awards no point.
/// </summary>
/// <param name="PackId">The valid pack of <see cref="GameState.Catalog"/> previewed.</param>
/// <param name="Pack">Its descriptor, copied when the preview starts.</param>
/// <param name="Media">
/// Its media files, under identifiers drawn when the preview starts, which the server serves them by as during a game.
/// </param>
/// <param name="RoundIds">An identifier for each round of <paramref name="Pack"/>, for the incidents of the TV screen.</param>
/// <param name="RoundIndex">The round shown, from 0.</param>
/// <param name="StepIndex">The step of that round shown, from 0.</param>
/// <param name="ExcerptStartsAt">
/// When the TV screen plays the excerpt of the step, in server time, or <see langword="null"/> while it stands still.
/// </param>
public sealed record PackPreview(
    string PackId,
    PackDescriptor Pack,
    PackMedia Media,
    ImmutableArray<RoundId> RoundIds,
    int RoundIndex,
    int StepIndex,
    DateTimeOffset? ExcerptStartsAt)
{
    /// <summary>
    /// The round shown, as every role knows a round.
    /// </summary>
    public RoundInfo Round
    {
        get
        {
            var descriptor = Pack.Rounds[RoundIndex];
            return new(RoundIds[RoundIndex], RoundIndex + 1, Pack.Rounds.Length, descriptor.Title, GameModes.TypeOf(descriptor), descriptor.Description);
        }
    }
}
