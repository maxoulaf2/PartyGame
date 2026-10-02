using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// The last open connection of a player closed. The hub counts the connections of each player and only reports this
/// transition, so that a player with two tabs stays connected while one of them is open.
/// </summary>
/// <param name="PlayerId">The player whose last connection closed.</param>
public sealed record PlayerConnectionLost(PlayerId PlayerId) : GameInput;
