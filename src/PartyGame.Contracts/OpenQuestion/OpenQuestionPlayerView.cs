namespace PartyGame.Contracts.OpenQuestion;

/// <summary>
/// What a round of open questions shows on the phone of one player.
/// </summary>
/// <remarks>
/// Empty until the questions are played (US-E16-02): the round finishes as soon as it starts.
/// </remarks>
public sealed record OpenQuestionPlayerView : PlayerRoundView;
