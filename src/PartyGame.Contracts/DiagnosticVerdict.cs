namespace PartyGame.Contracts;

/// <summary>
/// The outcome of a network diagnostic, as the page that ran it decided from its measures.
/// </summary>
public enum DiagnosticVerdict
{
    /// <summary>Every measure is good: the phone can play.</summary>
    Good,

    /// <summary>The phone can play, but a measure is poor: answers or the buzzer may lag.</summary>
    Reserved,

    /// <summary>The phone cannot play in these conditions, such as when the hub is out of reach.</summary>
    Problem,
}
