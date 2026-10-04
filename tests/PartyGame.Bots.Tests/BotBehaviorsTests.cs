namespace PartyGame.Bots.Tests;

public sealed class BotBehaviorsTests
{
    [Fact]
    public void Parse_SingleBehavior_AppliesToEveryBot() =>
        Assert.Equal([BotBehavior.Slow, BotBehavior.Slow, BotBehavior.Slow], BotBehaviors.Parse("slow", 3));

    [Fact]
    public void Parse_NoCount_MakesOneBot() =>
        Assert.Equal([BotBehavior.Random], BotBehaviors.Parse("random", null));

    [Fact]
    public void Parse_Mix_ListsEachBehaviorAsManyTimesAsAsked() =>
        Assert.Equal(
            [BotBehavior.Random, BotBehavior.Random, BotBehavior.Flaky, BotBehavior.Silent],
            BotBehaviors.Parse("random:2,flaky:1,SILENT:1", 4));

    [Theory]
    [InlineData("random:2,flaky:1", 4)]
    [InlineData("random:0", null)]
    [InlineData("random:2,flaky", null)]
    [InlineData("sleepy", null)]
    [InlineData("7", null)]
    public void Parse_InvalidOrMismatchedSpec_ReturnsNull(string spec, int? count) =>
        Assert.Null(BotBehaviors.Parse(spec, count));
}
