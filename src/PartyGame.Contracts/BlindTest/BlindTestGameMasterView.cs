namespace PartyGame.Contracts.BlindTest;

/// <summary>
/// What a blind test round shows on the game master console.
/// </summary>
/// <remarks>
/// Empty until the tracks are played (US-E15-02): the round finishes as soon as it starts.
/// </remarks>
public sealed record BlindTestGameMasterView : GameMasterRoundView;
