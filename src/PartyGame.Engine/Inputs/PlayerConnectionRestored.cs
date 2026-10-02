using PartyGame.Contracts;

namespace PartyGame.Engine.Inputs;

/// <summary>
/// A player who had no open connection resumed their session on a new one, presenting their token. The hub counts the
/// connections of each player and only reports this transition, like <see cref="PlayerConnectionLost"/>.
/// </summary>
/// <param name="PlayerId">The player who is connected again.</param>
public sealed record PlayerConnectionRestored(PlayerId PlayerId) : GameInput;
