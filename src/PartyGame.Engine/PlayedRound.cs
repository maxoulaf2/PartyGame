using PartyGame.Contracts;
using PartyGame.Engine.Modes;

namespace PartyGame.Engine;

/// <summary>
/// A round of the game: an activity of the pack, played by its game mode.
/// </summary>
/// <param name="Id">Identifier of the round, generated when it starts.</param>
/// <param name="Index">Position of its activity in <see cref="GameState.Rounds"/>, from 0.</param>
/// <param name="State">State of the round, proper to its game mode.</param>
public sealed record PlayedRound(RoundId Id, int Index, RoundState State);
