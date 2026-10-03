using PartyGame.Contracts;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// The viewers a secret is hidden from.
/// </summary>
internal sealed class Audience
{
    private readonly Func<Viewer, bool> _includes;
    private readonly string _description;

    private Audience(Func<Viewer, bool> includes, string description)
    {
        _includes = includes;
        _description = description;
    }

    /// <summary>Every client, the game master console included: player tokens, paths of the media files.</summary>
    public static Audience Everyone { get; } = new(_ => true, "everyone");

    /// <summary>The TV screen and every phone: the answers before their reveal, what only the game master manages.</summary>
    public static Audience AllButGameMaster { get; } = new(v => v.Role != Role.GameMaster, "all but the game master");

    /// <summary>The phones of the other players: what a player sees of themselves only.</summary>
    public static Audience OtherPlayersThan(string player) =>
        new(v => v.Role == Role.Player && v.Player != player, $"the players other than {player}");

    /// <summary>The TV screen and the phones of the other players: the choice of a player before the reveal.</summary>
    public static Audience AllButGameMasterAnd(string player) =>
        new(v => v.Role == Role.Display || (v.Role == Role.Player && v.Player != player), $"all but the game master and {player}");

    public bool Includes(Viewer viewer) => _includes(viewer);

    public override string ToString() => _description;
}
