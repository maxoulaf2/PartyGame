namespace PartyGame.Engine.Modes;

/// <summary>
/// State of a round, proper to the game mode that plays it. Immutable like the rest of the game state: a mode derives its
/// own <c>record</c> from this one and produces new instances with <c>with</c>.
/// </summary>
public abstract record RoundState;
