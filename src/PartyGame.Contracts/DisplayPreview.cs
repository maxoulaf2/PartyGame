namespace PartyGame.Contracts;

/// <summary>
/// The step of a pack the TV screen previews, at the request of the game master in the lobby: shown as during a game, and
/// already revealed. No player takes part in it.
/// </summary>
/// <param name="Round">The round of the pack the step belongs to.</param>
/// <param name="Step">Where the step stands among those of its round.</param>
/// <param name="View">What the game mode of the round shows on the TV screen at this step, revealed.</param>
public sealed record DisplayPreview(RoundInfo Round, RoundStep Step, DisplayRoundView View);
