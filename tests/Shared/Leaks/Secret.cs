namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// A value that no projection shown to <paramref name="HiddenFrom"/> may contain, in any string or property name.
/// </summary>
/// <param name="Value">A value distinctive enough not to appear by chance: a token, a marker text.</param>
/// <param name="HiddenFrom">The viewers it is hidden from.</param>
internal sealed record Secret(string Value, Audience HiddenFrom);
