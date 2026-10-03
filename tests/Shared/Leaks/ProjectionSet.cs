using System.Text.Json;
using System.Text.Json.Nodes;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Tests.Shared.Leaks;

/// <summary>
/// What each viewer receives for one state, serialized as the hub does: the leaks are looked for in the JSON a client
/// actually gets, not in the objects.
/// </summary>
internal sealed class ProjectionSet
{
    /// <param name="display">The snapshot of the TV screen.</param>
    /// <param name="gameMaster">The snapshot of the game master console.</param>
    /// <param name="players">The snapshot of each phone, by the name of its player.</param>
    /// <param name="options">The serializer options, <see cref="ContractJsonOptions.Default"/> unless the test registers types of its own.</param>
    public ProjectionSet(object display, object gameMaster, IEnumerable<(string Player, object Snapshot)> players, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        options ??= ContractJsonOptions.Default;
        List<(Viewer, JsonNode?)> all = [(Viewer.Display, Serialize(display, options)), (Viewer.GameMaster, Serialize(gameMaster, options))];
        foreach (var (player, snapshot) in players)
        {
            if (all.Any(p => p.Item1.Player == player))
            {
                throw new ArgumentException($"Two players are named {player}: the failure messages could not tell them apart.", nameof(players));
            }

            all.Add((Viewer.PhoneOf(player), Serialize(snapshot, options)));
        }

        All = all;
    }

    /// <summary>Every viewer and its JSON, the TV screen first, then the game master, then the phones.</summary>
    public IReadOnlyList<(Viewer Viewer, JsonNode? Json)> All { get; }

    public JsonNode? For(Viewer viewer) => All.FirstOrDefault(p => p.Viewer == viewer).Json;

    public bool Has(Viewer viewer) => All.Any(p => p.Viewer == viewer);

    private static JsonNode? Serialize(object snapshot, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.SerializeToNode(snapshot, snapshot.GetType(), options);
    }
}
