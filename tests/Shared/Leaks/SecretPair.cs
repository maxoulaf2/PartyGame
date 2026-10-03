namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// Two states that differ only by a secret, such as the correct answer or the choice of a player: the projections shown
/// to <paramref name="HiddenFrom"/> must be the same for both, down to an index, an order or a count.
/// </summary>
/// <param name="Secret">What tells the two states apart, named for the failure messages.</param>
/// <param name="One">A state with one value of the secret.</param>
/// <param name="Other">The same state with another value of the secret, or without it.</param>
/// <param name="HiddenFrom">The viewers the secret is hidden from.</param>
/// <typeparam name="TState">The state the projections are made from.</typeparam>
internal sealed record SecretPair<TState>(string Secret, TState One, TState Other, Audience HiddenFrom);
