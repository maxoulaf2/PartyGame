namespace PartyGame.Contracts;

/// <summary>
/// The answer of the hub to a <see cref="RecoverSessionRequest"/>: the token of the player, which the phone then keeps and
/// presents in a <see cref="ResumeSessionRequest"/> like after any reconnection. Sent to the phone that asked and to nobody
/// else.
/// </summary>
/// <param name="Refusal">Why the code was refused, or <see langword="null"/> when it was recognized.</param>
/// <param name="Token">The token of the player the code designates, or <see langword="null"/> when refused.</param>
/// <param name="LastClientSeq">
/// The number of the last intent of the player the server handled, 0 when refused: the phone numbers its intents from the
/// next one, so that the server does not ignore them as already handled.
/// </param>
public sealed record RecoverSessionResult(RecoverSessionRefusal? Refusal, string? Token, long LastClientSeq);
