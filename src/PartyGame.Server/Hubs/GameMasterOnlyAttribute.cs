namespace PartyGame.Server.Hubs;

/// <summary>
/// Marks a hub method that only a connection authenticated as game master may call. Checked by
/// <see cref="GameMasterOnlyFilter"/>, for every hub method at once.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class GameMasterOnlyAttribute : Attribute;
