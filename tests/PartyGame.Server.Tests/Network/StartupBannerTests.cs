using System.Collections.Immutable;
using System.Net;
using PartyGame.Content;
using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Network;
using static PartyGame.Server.Tests.Network.TestInterfaces;

namespace PartyGame.Server.Tests.Network;

public sealed class StartupBannerTests
{
    private static readonly GameMasterCode _generatedCode = GameMasterCode.Create(new GameMasterOptions());

    private static readonly PackLibrary _packs = new("/srv/partygame/packs", DirectoryExists: true, [Pack("quiz-exemple", "Quiz d'exemple", 2)]);

    [Fact]
    public void Format_DetectedAddress_GivesScreenUrls()
    {
        var banner = StartupBanner.Format(AddressSelector.Select([Wifi("192.168.1.42")], configured: null), port: 5000, _generatedCode, _packs);

        Assert.Contains("192.168.1.42 (détectée)", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.1.42:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.1.42:5000/gm/", banner, StringComparison.Ordinal);
        Assert.DoesNotContain("Autres adresses", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SeveralCandidates_ListsOthersAndHowToImposeOne()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42"), Ethernet("10.0.0.2", hasGateway: false, name: "Ethernet 2")], configured: null);

        var banner = StartupBanner.Format(selection, port: 5001, _generatedCode, _packs);

        Assert.Contains("http://192.168.1.42:5001/display/", banner, StringComparison.Ordinal);
        Assert.Contains("10.0.0.2 (Ethernet 2)", banner, StringComparison.Ordinal);
        Assert.Contains("--Network:AdvertisedAddress=10.0.0.2", banner, StringComparison.Ordinal);
        Assert.Contains("Network__AdvertisedAddress=10.0.0.2", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ConfiguredAddressOnNoInterface_SaysWhereItComesFromAndWarns()
    {
        var selection = AddressSelector.Select([Wifi("192.168.1.42")], IPAddress.Parse("192.168.50.7"));

        var banner = StartupBanner.Format(selection, port: 5000, _generatedCode, _packs);

        Assert.Contains("192.168.50.7 (imposée par Network:AdvertisedAddress)", banner, StringComparison.Ordinal);
        Assert.Contains("aucune interface réseau active", banner, StringComparison.Ordinal);
        Assert.Contains("http://192.168.50.7:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("192.168.1.42 (Wi-Fi)", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_NoAddress_AsksToConnectAndGivesLocalUrls()
    {
        var banner = StartupBanner.Format(AddressSelector.Select([], configured: null), port: 5000, _generatedCode, _packs);

        Assert.Contains("Connectez ce PC au Wi-Fi", banner, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5000/display/", banner, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5000/gm/", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_GeneratedCode_ShowsItNextToTheScreenUrls()
    {
        var banner = StartupBanner.Format(AddressSelector.Select([Wifi("192.168.1.42")], configured: null), port: 5000, _generatedCode, _packs);

        Assert.Contains($"Code game master    : {_generatedCode.RevealForBanner()}{Environment.NewLine}", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ConfiguredCode_SaysWhereItComesFrom()
    {
        var code = GameMasterCode.Create(new GameMasterOptions { Code = "482913" });

        var banner = StartupBanner.Format(AddressSelector.Select([Wifi("192.168.1.42")], configured: null), port: 5000, code, _packs);

        Assert.Contains("Code game master    : 482913 (imposé par GameMaster:Code)", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_Packs_ListsEachWithItsTitleRoundsAndState()
    {
        var problem = new PackProblem(PackProblemCode.PackPropertyMissing, "pack.json", "$", ImmutableDictionary<string, string>.Empty);
        var packs = new PackLibrary("/srv/partygame/packs", DirectoryExists: true,
        [
            Pack("quiz-exemple", "Quiz d'exemple", 2),
            Pack("soiree", "Soirée", 1, problem, problem, problem),
            Pack("casse", title: null, roundCount: null, problem),
        ]);

        var banner = Format(packs);

        Assert.Contains("Packs (/srv/partygame/packs) :", banner, StringComparison.Ordinal);
        Assert.Contains("    quiz-exemple        : « Quiz d'exemple », 2 manches, valide", banner, StringComparison.Ordinal);
        Assert.Contains("    soiree              : « Soirée », 1 manche, invalide (3 problèmes)", banner, StringComparison.Ordinal);
        Assert.Contains("    casse               : sans titre, invalide (1 problème)", banner, StringComparison.Ordinal);
        Assert.Contains("détaillés dans le journal", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ValidPacksOnly_DoesNotMentionProblems()
    {
        var banner = Format(_packs);

        Assert.DoesNotContain("journal", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_MissingPackDirectory_SaysSoAndHowToSetIt()
    {
        var banner = Format(new PackLibrary("/srv/partygame/packs", DirectoryExists: false, []));

        Assert.Contains("Aucun pack : le dossier /srv/partygame/packs est introuvable.", banner, StringComparison.Ordinal);
        Assert.Contains("--Packs:Directory=", banner, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_EmptyPackDirectory_SaysWhatAPackIs()
    {
        var banner = Format(new PackLibrary("/srv/partygame/packs", DirectoryExists: true, []));

        Assert.Contains("Aucun pack dans le dossier /srv/partygame/packs.", banner, StringComparison.Ordinal);
        Assert.Contains("pack.json", banner, StringComparison.Ordinal);
    }

    private static string Format(PackLibrary packs) =>
        StartupBanner.Format(AddressSelector.Select([Wifi("192.168.1.42")], configured: null), port: 5000, _generatedCode, packs);

    private static LoadedPack Pack(string id, string? title, int? roundCount, params PackProblem[] problems) =>
        new(id, $"/srv/partygame/packs/{id}", title, roundCount, problems.Length == 0 ? new PackDescriptor { FormatVersion = 1, Title = title!, Rounds = [] } : null, [.. problems]);
}
