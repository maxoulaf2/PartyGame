using PartyGame.Engine.Tests.Rounds;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Projections;

/// <summary>
/// What the snapshots of the game, outside the views of the modes, may show to each viewer: the tokens to nobody, the
/// networks of the host and the packs to the game master only, the rounds as they are played, and nothing of a player to
/// the others.
/// </summary>
public sealed class SnapshotsLeakTests
{
    private static readonly LeakSuite<GameState, GamePhase> _suite = new()
    {
        PhaseOf = state => state.Phase,
        Project = Games.Projected,
        Scenarios = [.. Enum.GetValues<GamePhase>().SelectMany(Scenarios)],
        SecretsOf = SecretsOf,
        Pairs = [.. Enum.GetValues<GamePhase>().SelectMany(Pairs)],
    };

    [Fact]
    public void Snapshots_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void Snapshots_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void Snapshots_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    private static IEnumerable<(string, GameState)> Scenarios(GamePhase phase)
    {
        yield return ("three players", Games.InPhase(phase, "Zoé", "Max", "Léa"));

        var neighbour = Games.ValidPack("pack-voisin", "Titre voisin", [new FakeRoundDescriptor { Title = "Manche voisine" }]);
        var packs = Games.Loaded(Games.Pack, neighbour, Games.InvalidPack("pack-casse", "Titre cassé"));
        yield return ("other packs", Games.PlayedUpTo(phase, Games.Accepted(Games.LobbyWith("Zoé", "Max"), packs)));

        yield return ("illustrated pack", Games.PlayedUpTo(phase, Games.IllustratedLobbyWith("Zoé", "Max")));

        yield return ("other address chosen", Games.PlayedUpTo(phase, Games.Accepted(Games.LobbyWith("Zoé"), Games.ChooseAddress(Games.OtherAddress))));
    }

    private static IEnumerable<SecretPair<GameState>> Pairs(GamePhase phase)
    {
        var state = Games.InPhase(phase, "Zoé", "Max");
        yield return new SecretPair<GameState>("tokens", state, state with { PlayerTokens = state.PlayerTokens.Clear() }, Audience.Everyone);

        // Everything the TV screen and the phones may not show, down to the other packs and the title of the chosen one
        var withoutSecrets = state with
        {
            PlayerTokens = state.PlayerTokens.Clear(),
            JoinAddressCandidates = [],
            Catalog = new PackCatalog(string.Empty, [new CatalogPack(Games.PackId, Games.Pack.Title, RoundCount: null, Descriptor: null, [])]),
        };
        yield return new SecretPair<GameState>("secrets of the game master", state, withoutSecrets, Audience.AllButGameMaster);
    }

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        foreach (var token in state.PlayerTokens.Keys)
        {
            yield return new Secret(token.Value, Audience.Everyone);
        }

        // The networks of the host: only the advertised address reaches the TV screen.
        foreach (var candidate in state.JoinAddressCandidates)
        {
            if (candidate.Address != state.JoinAddress)
            {
                yield return new Secret(candidate.Address, Audience.AllButGameMaster);
            }

            if (candidate.InterfaceName is { } name)
            {
                yield return new Secret(name, Audience.AllButGameMaster);
            }
        }

        // The packs: only the title of the chosen one reaches the TV screen.
        var chosen = state.Pack ?? (state.SelectedPackId is { } id ? state.Catalog.Find(id)?.Descriptor : null);
        var chosenRounds = chosen?.Rounds.Select(r => r.Title).ToHashSet(StringComparer.Ordinal) ?? [];
        if (state.Catalog.Directory.Length > 0)
        {
            yield return new Secret(state.Catalog.Directory, Audience.AllButGameMaster);
        }

        foreach (var pack in state.Catalog.Packs)
        {
            yield return new Secret(pack.Id, Audience.AllButGameMaster);
            if (pack.Id != state.SelectedPackId && pack.Title is { } title)
            {
                yield return new Secret(title, Audience.AllButGameMaster);
            }

            foreach (var round in pack.Descriptor?.Rounds.Where(r => !chosenRounds.Contains(r.Title)) ?? [])
            {
                yield return new Secret(round.Title, Audience.AllButGameMaster);
            }

            foreach (var problem in pack.Problems)
            {
                yield return new Secret(problem.Code.ToString(), Audience.AllButGameMaster);
                yield return new Secret(problem.Path, Audience.AllButGameMaster);
                foreach (var parameter in problem.Parameters.Values)
                {
                    yield return new Secret(parameter, Audience.AllButGameMaster);
                }
            }
        }

        // The rounds of the game are discovered as they are played.
        var played = state.CurrentRound?.Index ?? -1;
        foreach (var round in chosen?.Rounds.Skip(played + 1) ?? [])
        {
            yield return new Secret(round.Title, Audience.AllButGameMaster);
        }

        // A media file is named after what it shows, which may be the answer.
        var media = state.Media.Files.Values.Concat(state.Catalog.Packs.Where(p => p.Id == state.SelectedPackId).SelectMany(p => p.Media));
        foreach (var path in media.Select(m => m.Value).Distinct())
        {
            yield return new Secret(path, Audience.Everyone);
            foreach (var part in path.Split('/'))
            {
                yield return new Secret(part, Audience.Everyone);
                yield return new Secret(Path.GetFileNameWithoutExtension(part), Audience.Everyone);
            }
        }

        foreach (var player in state.Players)
        {
            yield return new Secret(player.Nickname, Audience.OtherPlayersThan(player.Nickname));
            yield return new Secret(player.Id.Value.ToString(), Audience.OtherPlayersThan(player.Nickname));
        }
    }
}
