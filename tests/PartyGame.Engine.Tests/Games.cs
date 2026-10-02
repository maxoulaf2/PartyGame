using System.Collections.Immutable;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Projections;
using PartyGame.Engine.Tests.Rounds;

namespace PartyGame.Engine.Tests;

/// <summary>
/// Builds the states, inputs and contexts that engine tests start from.
/// </summary>
internal static class Games
{
    public static readonly DateTimeOffset Now = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    /// <summary>The modes of the tests: only <see cref="FakeMode"/>, registered for its own activities.</summary>
    public static readonly GameModes Modes = new([new FakeMode()]);

    public static readonly GameEngine Engine = new(Modes);

    public static readonly Snapshots Snapshots = new(Modes);

    public const string JoinAddress = "192.168.1.42";

    public const string OtherAddress = "10.0.0.2";

    /// <summary>A host on the Wi-Fi of the venue and on a wired network without gateway.</summary>
    public static readonly ImmutableArray<JoinAddressCandidate> JoinAddressCandidates =
        [new(JoinAddress, "Wi-Fi"), new(OtherAddress, "Ethernet")];

    /// <summary>Two activities played by <see cref="FakeMode"/>.</summary>
    public static readonly ImmutableArray<RoundDescriptor> TwoRounds =
        [new FakeRoundDescriptor { Title = "Échauffement" }, new FakeRoundDescriptor { Title = "Finale" }];

    public const string PackDirectory = "/srv/partygame/packs";

    /// <summary>The identifier of <see cref="Pack"/>, the only pack of <see cref="Catalog"/>.</summary>
    public const string PackId = "soiree";

    /// <summary>A pack of <see cref="TwoRounds"/>.</summary>
    public static readonly CatalogPack Pack = ValidPack(PackId, "Soirée test", TwoRounds);

    /// <summary>A catalog with a single valid pack, <see cref="Pack"/>, which a new game chooses at once.</summary>
    public static readonly PackCatalog Catalog = new(PackDirectory, [Pack]);

    public static GameState NewLobby() =>
        GameState.Create(new GameId(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff")), JoinAddress, JoinAddressCandidates, Catalog);

    public static CatalogPack ValidPack(string id, string title, ImmutableArray<RoundDescriptor> rounds) =>
        new(id, title, rounds.Length, new PackDescriptor { FormatVersion = 1, Title = title, Rounds = rounds }, []);

    /// <summary>A pack with a missing media file and a question without correct choice.</summary>
    public static CatalogPack InvalidPack(string id, string title = "Pack cassé") =>
        new(
            id,
            title,
            RoundCount: 1,
            Descriptor: null,
            [
                new PackProblem(PackProblemCode.PackMediaMissing, PackDescriptor.FileName, "$.rounds[0].questions[0].image", ImmutableDictionary<string, string>.Empty.Add("media", "images/tour-eiffel.jpg")),
                new PackProblem(PackProblemCode.QuizCorrectChoiceMissing, PackDescriptor.FileName, "$.rounds[0].questions[1]", ImmutableDictionary<string, string>.Empty),
            ]);

    public static SelectPack Select(string packId) => new(packId, Now);

    public static PacksLoaded Loaded(params CatalogPack[] packs) => new(new PackCatalog(PackDirectory, [.. packs]));

    public static GameContext Context(int seed = 42) => new(Now, new Random(seed));

    public static JoinGame Join(string nickname, int player = 1) =>
        new(PlayerIdOf(player), new PlayerToken($"token-{player}"), nickname, Now);

    public static RenamePlayer Rename(int player, string nickname) => new(PlayerIdOf(player), nickname, Now);

    public static StartGame Start() => new(Now);

    public static ChooseAdvertisedAddress ChooseAddress(string address) => new(address, Now);

    public static PlayerRoundInput PlayerActs(GameState state, int player, string action) =>
        new(PlayerIdOf(player), new FakePlayerIntent(state.CurrentRound!.Id, action), Now);

    public static GameMasterRoundInput GameMasterActs(GameState state, string action) =>
        new(new FakeGameMasterIntent(state.CurrentRound!.Id, action), Now);

    public static NextRound NextRound(GameState state) => new(state.CurrentRound!.Id, Now);

    public static PlayerId PlayerIdOf(int player) => new(new Guid(player, 0, 0, new byte[8]));

    /// <summary>
    /// A lobby with the given players, registered in order.
    /// </summary>
    public static GameState LobbyWith(params string[] nicknames)
    {
        var state = NewLobby();
        for (var i = 0; i < nicknames.Length; i++)
        {
            state = Engine.Handle(state, Join(nicknames[i], player: i + 1), Context()).State;
        }

        return state;
    }

    /// <summary>
    /// A game of <see cref="Pack"/> with the given players, in the given phase: the first round in progress, between
    /// the two rounds, or finished.
    /// </summary>
    public static GameState InPhase(GamePhase phase, params string[] nicknames) => PlayedUpTo(phase, LobbyWith(nicknames));

    /// <summary>
    /// Plays a lobby whose selected pack has two rounds up to the given phase: the first round in progress, between the two
    /// rounds, or finished.
    /// </summary>
    public static GameState PlayedUpTo(GamePhase phase, GameState lobby)
    {
        if (phase == GamePhase.Lobby)
        {
            return lobby;
        }

        var state = Accepted(lobby, Start());
        if (phase == GamePhase.Round)
        {
            return state;
        }

        state = Accepted(state, GameMasterActs(state, FakeGameMasterIntent.Finish));
        if (phase == GamePhase.BetweenRounds)
        {
            return state;
        }

        state = Accepted(state, NextRound(state), seed: 43);
        return Accepted(state, GameMasterActs(state, FakeGameMasterIntent.Finish));
    }

    /// <summary>
    /// The state after an input the test expects to be accepted.
    /// </summary>
    /// <param name="state">The state to handle the input in.</param>
    /// <param name="input">The input.</param>
    /// <param name="seed">
    /// Seed of the context. The loop draws every round identifier from one generator: a test that starts a second round
    /// gives it another seed, so that it gets another identifier.
    /// </param>
    public static GameState Accepted(GameState state, GameInput input, int seed = 42)
    {
        var transition = Engine.Handle(state, input, Context(seed));
        Assert.Null(transition.Rejection);
        return transition.State;
    }
}
