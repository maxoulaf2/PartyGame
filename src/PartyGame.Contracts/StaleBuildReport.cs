namespace PartyGame.Contracts;

/// <summary>
/// A page tells the server that it still runs another build than the one served after reloading to get it: a stubborn
/// cache or a proxy. The page goes on with its build, and the operator finds the report in the logs.
/// </summary>
/// <param name="ClientBuildId">The identifier of the build the page runs.</param>
public sealed record StaleBuildReport(string ClientBuildId);
