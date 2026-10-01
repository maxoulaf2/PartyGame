namespace PartyGame.Server.Games;

/// <summary>
/// Status of an input handled by the loop.
/// </summary>
internal enum InputStatus
{
    /// <summary>The engine accepted the input.</summary>
    Accepted,

    /// <summary>The engine rejected the input by the rules: the state is unchanged.</summary>
    Rejected,

    /// <summary>The engine threw: a bug, the previous state is kept.</summary>
    Failed,
}
