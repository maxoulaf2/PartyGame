namespace PartyGame.Contracts;

/// <summary>
/// A page tells the server about a JavaScript error nobody saw on screen, so that the operator finds it in the logs. It
/// carries neither nickname, nor token, nor game master code: the server adds the player it knows the connection as.
/// </summary>
/// <param name="Role">The role of the page, which may not have announced it yet.</param>
/// <param name="Page">The path of the page, without its query nor its fragment.</param>
/// <param name="Kind">How the error came to light.</param>
/// <param name="Message">The message of the error.</param>
/// <param name="Stack">The beginning of its stack trace, or <see langword="null"/> when the browser gives none.</param>
/// <param name="RoundViewType">
/// The <c>type</c> of the round view the page showed, or <see langword="null"/> outside a round.
/// </param>
/// <param name="SnapshotVersion">The version of the snapshot the page showed, or <see langword="null"/> before the first.</param>
/// <param name="BuildId">The client build of the page, or <see langword="null"/> outside of <c>npm run build</c>.</param>
public sealed record ClientErrorReport(
    Role Role,
    string Page,
    ClientErrorKind Kind,
    string Message,
    string? Stack,
    string? RoundViewType,
    long? SnapshotVersion,
    string? BuildId);
