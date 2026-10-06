using PartyGame.Contracts;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine;

/// <summary>
/// A round of the game: an activity of the pack, played by its game mode.
/// </summary>
/// <param name="Id">Identifier of the round, generated when it is announced.</param>
/// <param name="Index">Position of its activity in <see cref="GameState.Rounds"/>, from 0.</param>
/// <param name="State">
/// State of the round, proper to its game mode, or <see langword="null"/> while the round is announced and its mode has
/// not started it yet.
/// </param>
public sealed record PlayedRound(RoundId Id, int Index, RoundState? State)
{
    /// <summary>
    /// Whether the game master skipped the round, which then ended without its game mode: <see cref="State"/> stays as
    /// the mode last left it, in the middle of the round.
    /// </summary>
    public bool IsSkipped { get; init; }
}
