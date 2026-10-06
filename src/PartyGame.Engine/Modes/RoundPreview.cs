namespace PartyGame.Engine.Modes;

/// <summary>
/// A step of the preview of an activity, as its game mode builds it: the state of a round at that step, revealed, without
/// any player.
/// </summary>
/// <param name="Round">The round at that step, which the engine projects for the TV screen as during a game.</param>
/// <param name="HasExcerpt">Whether the step has an excerpt the TV screen can play.</param>
public sealed record RoundPreview(RoundState Round, bool HasExcerpt);
