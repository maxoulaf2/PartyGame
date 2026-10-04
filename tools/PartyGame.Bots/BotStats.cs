namespace PartyGame.Bots;

/// <summary>
/// What the bots of a run did, shared by all of them: they update it from their own threads.
/// </summary>
internal sealed class BotStats
{
    private readonly Lock _gate = new();
    private readonly List<double> _delays = [];
    private int _sent;
    private int _rejected;
    private int _reconnections;

    public int Sent => Volatile.Read(ref _sent);

    public int Rejected => Volatile.Read(ref _rejected);

    public int Reconnections => Volatile.Read(ref _reconnections);

    public void IntentSent() => Interlocked.Increment(ref _sent);

    public void IntentRejected() => Interlocked.Increment(ref _rejected);

    public void Reconnected() => Interlocked.Increment(ref _reconnections);

    /// <summary>
    /// Records the time between the sending of an intent and the snapshot that reflects it.
    /// </summary>
    public void BroadcastDelay(TimeSpan delay)
    {
        lock (_gate)
        {
            _delays.Add(delay.TotalMilliseconds);
        }
    }

    public string Summary(int connected, int total)
    {
        double[] delays;
        lock (_gate)
        {
            delays = [.. _delays.Order()];
        }

        var broadcast = delays.Length == 0
            ? "délai de diffusion : aucune mesure"
            : $"délai de diffusion : médiane {Percentile(delays, 50):0} ms, 95e centile {Percentile(delays, 95):0} ms";
        return $"{connected}/{total} bots connectés · {Sent} intentions envoyées · {Rejected} rejetées · {Reconnections} reconnexions · {broadcast}";
    }

    /// <summary>The nearest-rank percentile of sorted values.</summary>
    private static double Percentile(double[] sorted, int percent) =>
        sorted[Math.Max(0, (int)Math.Ceiling(percent / 100.0 * sorted.Length) - 1)];
}
