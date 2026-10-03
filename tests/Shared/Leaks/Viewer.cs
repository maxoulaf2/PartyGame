using PartyGame.Contracts;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// Who receives a projection: the TV screen, the game master console, or the phone of one player.
/// </summary>
/// <param name="Role">The role of the client.</param>
/// <param name="Player">The player whose phone it is, named as the test names them; <see langword="null"/> for the other roles.</param>
internal readonly record struct Viewer(Role Role, string? Player = null)
{
    public static Viewer Display { get; } = new(Role.Display);

    public static Viewer GameMaster { get; } = new(Role.GameMaster);

    public static Viewer PhoneOf(string player) => new(Role.Player, player);

    public override string ToString() => Player is null ? Role.ToString() : $"{Role} {Player}";
}
