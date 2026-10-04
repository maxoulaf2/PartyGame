namespace PartyGame.Bots;

/// <summary>
/// A failure the operator can fix, its message written for them: the tool shows it and stops, without a stack trace.
/// </summary>
internal sealed class BotException(string message) : Exception(message);
