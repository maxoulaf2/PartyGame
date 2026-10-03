namespace PartyGame.Server.Hubs;

/// <summary>
/// What becomes of an error report under the <see cref="ClientErrorAllowance"/> of its connection.
/// </summary>
internal enum ClientErrorAdmission
{
    /// <summary>Within the allowance: the report is logged.</summary>
    Admitted,

    /// <summary>The first report beyond the allowance: it is dropped, and the operator told so, once.</summary>
    FirstDropped,

    /// <summary>Another report beyond the allowance: dropped silently.</summary>
    Dropped,
}
