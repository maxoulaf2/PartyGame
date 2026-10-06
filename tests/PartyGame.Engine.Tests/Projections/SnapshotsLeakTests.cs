using PartyGame.Contracts;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Projections;
using PartyGame.Engine.Tests.Rounds;
using PartyGame.Tests.Shared.Leaks;

namespace PartyGame.Engine.Tests.Projections;

/// <summary>
/// What the snapshots of the game, outside the views of the modes, may show to each viewer: the tokens to nobody, the
/// reconnection codes, the networks of the host and the packs to the game master only, the rounds as they are played, and nothing of a player to
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

        // Nobody can register while the game master decides about the game found.
        Unreachable = [(GamePhase.ResumePending, Role.Player)],
    };

    [Fact]
    public void Snapshots_EveryPhase_IsCoveredForEachRole() => _suite.AssertEveryPhaseIsCovered();

    [Fact]
    public void Snapshots_AnyScenario_ShowNoSecretToWhomItIsHiddenFrom() => _suite.AssertNoSecretIsShown();

    [Fact]
    public void Snapshots_WithoutTheSecrets_LookTheSameToWhomTheyAreHiddenFrom() => _suite.AssertPairsLookTheSame();

    private static IEnumerable<(string, GameState)> Scenarios(GamePhase phase) =>
        phase == GamePhase.ResumePending ? PendingScenarios() : PlayedScenarios(phase);

    /// <summary>
    /// The games of every other scenario found saved in the middle of a round, and games found at other moments.
    /// </summary>
    private static IEnumerable<(string, GameState)> PendingScenarios()
    {
        foreach (var (name, state) in PlayedScenarios(GamePhase.Round))
        {
            yield return (name, Games.Pending(state));
        }

        // The files the game master must put back are named to them alone.
        yield return ("media missing", Games.Pending(Games.InPhase(GamePhase.Round, "Zoé"), Games.Flag));
        yield return ("illustrated game found", Games.Pending(Games.PlayedUpTo(GamePhase.Round, Games.IllustratedLobbyWith("Zoé", "Max")), Games.Monument));
        yield return ("lobby found", Games.Pending(Games.LobbyWith("Zoé", "Max")));
        yield return ("finished game found", Games.Pending(Games.InPhase(GamePhase.Finished, "Zoé", "Max")));
    }

    private static IEnumerable<(string, GameState)> PlayedScenarios(GamePhase phase)
    {
        yield return ("three players", Games.InPhase(phase, "Zoé", "Max", "Léa"));

        yield return ("tied scores", Games.WithScores(Games.InPhase(phase, "Zoé", "Max", "Léa"), 2000, 1000, 2000));

        // Once the game is finished, a late arrival is not ranked: neither are they shown to the others.
        yield return ("late arrival", Games.Accepted(Games.WithScores(Games.InPhase(phase, "Zoé", "Max"), 1000, 2000), Games.Join("Léa", player: 3)));

        var neighbour = Games.ValidPack("pack-voisin", "Titre voisin", [new FakeRoundDescriptor { Title = "Manche voisine" }]);
        var packs = Games.Loaded(Games.Pack, neighbour, Games.InvalidPack("pack-casse", "Titre cassé"));
        yield return ("other packs", Games.PlayedUpTo(phase, Games.Accepted(Games.LobbyWith("Zoé", "Max"), packs)));

        yield return ("illustrated pack", Games.PlayedUpTo(phase, Games.IllustratedLobbyWith("Zoé", "Max")));

        yield return ("described rounds", Games.PlayedUpTo(phase, DescribedLobby()));

        yield return ("other address chosen", Games.PlayedUpTo(phase, Games.Accepted(Games.LobbyWith("Zoé"), Games.ChooseAddress(Games.OtherAddress))));

        if (phase == GamePhase.Lobby)
        {
            // The TV screen previews a pack, the chosen one or another: the phones learn nothing of it.
            yield return ("preview of another pack", Previewed("illustre"));
            yield return ("preview of the chosen pack", Previewed(Games.PackId));
        }
    }

    /// <summary>
    /// A lobby where <see cref="Games.Pack"/> is chosen and the given pack previewed at its last step: the chosen one, or
    /// another one, illustrated, whose rounds are named apart.
    /// </summary>
    private static GameState Previewed(string packId)
    {
        var other = Games.ValidPack(
            "illustre",
            "Soirée illustrée",
            [new FakeRoundDescriptor { Title = "Manche aperçue", Image = Games.Flag }, new FakeRoundDescriptor { Title = "Dernière manche aperçue", Image = Games.Monument }]);
        var lobby = Games.Accepted(Games.LobbyWith("Zoé", "Max"), Games.Loaded(Games.Pack, other));
        var state = Games.Accepted(lobby, new StartPreview(packId, Games.Now));
        return Games.Accepted(state, new ShowPreviewStep(2, FakeMode.PreviewStepCount, PlayExcerpt: true, Games.Now));
    }

    /// <summary>
    /// A lobby where a pack of two rounds, each told of by its author, is chosen.
    /// </summary>
    private static GameState DescribedLobby()
    {
        var pack = Games.ValidPack(
            "decrite",
            "Soirée décrite",
            [
                new FakeRoundDescriptor { Title = "Échauffement", Description = "Pour se mettre en jambes." },
                new FakeRoundDescriptor { Title = "Finale", Description = "Tout se joue ici." },
            ]);
        return Games.Accepted(Games.Accepted(Games.LobbyWith("Zoé", "Max"), Games.Loaded(Games.Pack, pack)), Games.Select(pack.Id));
    }

    private static IEnumerable<SecretPair<GameState>> Pairs(GamePhase phase)
    {
        if (phase == GamePhase.ResumePending)
        {
            // The TV screen learns nothing of the game found: neither where it stopped, nor who played it.
            var found = Games.InPhase(GamePhase.Round, "Zoé", "Max");
            var another = Games.WithScores(Games.InPhase(GamePhase.BetweenRounds, "Léa"), 3000) with { Version = found.Version };
            yield return new SecretPair<GameState>("game found", Games.Pending(found), Games.Pending(another), Audience.AllButGameMaster);
            yield break;
        }

        if (phase == GamePhase.Lobby)
        {
            var previewed = Previewed("illustre");
            yield return new SecretPair<GameState>("preview", previewed, previewed with { Preview = null }, Audience.Players);
        }

        // Before the round starts, nothing of its content reaches anybody, not even the game master: only its title and
        // what the author tells of it.
        if (phase == GamePhase.RoundIntro)
        {
            var announced = Games.PlayedUpTo(phase, Games.IllustratedLobbyWith("Zoé", "Max"));
            var pack = announced.Pack!;
            var otherContent = announced with
            {
                Pack = pack with { Rounds = pack.Rounds.SetItem(0, ((FakeRoundDescriptor)pack.Rounds[0]) with { Image = Games.Monument }) },
            };
            yield return new SecretPair<GameState>("content of the round announced", announced, otherContent, Audience.Everyone);
        }

        var state = Games.InPhase(phase, "Zoé", "Max");
        yield return new SecretPair<GameState>("tokens", state, state with { PlayerTokens = state.PlayerTokens.Clear() }, Audience.Everyone);

        // Everything the TV screen and the phones may not show, down to the other packs and the title of the chosen one
        var withoutSecrets = state with
        {
            PlayerTokens = state.PlayerTokens.Clear(),
            ReconnectionCodes = state.ReconnectionCodes.Clear(),
            JoinAddressCandidates = [],
            Catalog = new PackCatalog(string.Empty, [new CatalogPack(Games.PackId, Games.Pack.Title, RoundCount: null, Descriptor: null, [])]),
        };
        yield return new SecretPair<GameState>("secrets of the game master", state, withoutSecrets, Audience.AllButGameMaster);

        // A round skipped by the game master is over like any other for the TV screen and the phones, whatever its mode
        // left in the middle of it.
        if (phase is GamePhase.BetweenRounds or GamePhase.Finished)
        {
            yield return new SecretPair<GameState>("skipped round", Skipped(phase), Games.InPhase(phase, "Zoé", "Max"), Audience.AllButGameMaster);
        }

        // A phone learns its own rank, never by how much the others lead.
        yield return new SecretPair<GameState>(
            "score of another player",
            Games.WithScores(state, 3000, 1000),
            Games.WithScores(state, 5000, 1000),
            Audience.OtherPlayersThan("Zoé"));
    }

    private static IEnumerable<Secret> SecretsOf(GameState state)
    {
        foreach (var token in state.PlayerTokens.Keys)
        {
            yield return new Secret(token.Value, Audience.Everyone);
        }

        // A reconnection code is as good as a token: the game master reads it to the player alone.
        foreach (var code in state.ReconnectionCodes.Values)
        {
            yield return new Secret(code, Audience.AllButGameMaster);
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

        // The packs: only the title of the chosen one reaches the TV screen, and the rounds of the one it previews.
        var chosen = state.Pack ?? (state.SelectedPackId is { } id ? state.Catalog.Find(id)?.Descriptor : null);
        Audience RoundsHiddenFrom(string? packId) => packId == state.Preview?.PackId ? Audience.Players : Audience.AllButGameMaster;
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
                yield return new Secret(round.Title, RoundsHiddenFrom(pack.Id));
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
            yield return new Secret(round.Title, RoundsHiddenFrom(state.Pack is null ? state.SelectedPackId : null));
            if (round.Description is { } description)
            {
                yield return new Secret(description, Audience.Everyone);
            }
        }

        // A media file is named after what it shows, which may be the answer.
        var media = state.Media.Files.Values
            .Concat(state.Catalog.Packs.Where(p => p.Id == state.SelectedPackId).SelectMany(p => p.Media))
            .Concat(state.Preview?.Media.Files.Values ?? []);
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

        // The game found: the game master learns where it stopped, the files missing and how many played it, never who.
        if (state.PendingGame is { } pending)
        {
            var missing = pending.MissingMedia.Select(m => m.Value).ToList();
            // Its addresses are those of the previous run: the TV screen shows the one of this startup, whatever they were.
            foreach (var secret in SecretsOf(pending.Game).Where(secret => secret.Value != state.JoinAddress))
            {
                var toGameMaster = secret.HiddenFrom == Audience.AllButGameMaster || missing.Any(m => m.Contains(secret.Value, StringComparison.Ordinal));
                yield return new Secret(secret.Value, toGameMaster ? Audience.AllButGameMaster : Audience.Everyone);
            }

            if (pending.Game.Pack?.Title is { } title)
            {
                yield return new Secret(title, Audience.AllButGameMaster);
            }

            if (Snapshots.RoundInfoOf(pending.Game) is { } round)
            {
                yield return new Secret(round.Title, Audience.AllButGameMaster);
            }
        }
    }

    /// <summary>
    /// The game of <see cref="Games.InPhase"/> whose last round played was skipped in the middle of it rather than ended by
    /// its mode.
    /// </summary>
    private static GameState Skipped(GamePhase phase)
    {
        var state = phase == GamePhase.BetweenRounds
            ? Games.InPhase(GamePhase.Round, "Zoé", "Max")
            : Games.InLastRound("Zoé", "Max");
        state = Games.Accepted(state, Games.GameMasterActs(state, "reveals"));
        return Games.Accepted(state, Games.SkipRound(state));
    }
}
