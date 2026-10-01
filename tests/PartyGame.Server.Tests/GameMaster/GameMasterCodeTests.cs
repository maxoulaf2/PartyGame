using PartyGame.Server.GameMaster;

namespace PartyGame.Server.Tests.GameMaster;

public sealed class GameMasterCodeTests
{
    [Fact]
    public void Create_NoCodeConfigured_GeneratesSixDigits()
    {
        var code = GameMasterCode.Create(new GameMasterOptions());

        Assert.Matches("^[0-9]{6}$", code.RevealForBanner());
        Assert.False(code.IsConfigured);
    }

    [Fact]
    public void Create_NoCodeConfigured_GeneratesADifferentCodeAtEachStart()
    {
        // Twenty draws all equal would happen by chance once in 10^114 runs.
        var codes = Enumerable.Range(0, 20).Select(_ => GameMasterCode.Create(new GameMasterOptions()).RevealForBanner());

        Assert.True(codes.Distinct(StringComparer.Ordinal).Count() > 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCodeConfigured_GeneratesOne(string configured)
    {
        var code = GameMasterCode.Create(new GameMasterOptions { Code = configured });

        Assert.Matches("^[0-9]{6}$", code.RevealForBanner());
        Assert.False(code.IsConfigured);
    }

    [Fact]
    public void Create_CodeConfigured_UsesIt()
    {
        var code = GameMasterCode.Create(new GameMasterOptions { Code = " 012345 " });

        Assert.Equal("012345", code.RevealForBanner());
        Assert.True(code.IsConfigured);
    }

    [Theory]
    [InlineData("482913", true)]
    [InlineData(" 482913 ", true)]
    [InlineData("482914", false)]
    [InlineData("48291", false)]
    [InlineData("4829130", false)]
    [InlineData("4829 13", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void Verify_Candidate_MatchesOnlyTheCode(string? candidate, bool expected)
    {
        var code = GameMasterCode.Create(new GameMasterOptions { Code = "482913" });

        Assert.Equal(expected, code.Verify(candidate));
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData(" 123456 ", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData("１２３４５６", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidCode_Configured_AcceptsOnlySixAsciiDigits(string? configured, bool expected) =>
        Assert.Equal(expected, GameMasterOptions.IsValidCode(configured));
}
