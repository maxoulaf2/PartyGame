using Microsoft.AspNetCore.SignalR;

namespace PartyGame.Server.Hubs;

/// <summary>
/// How many error reports one connection may have logged per minute. The pages already limit themselves; this bounds a
/// modified client, or a loop the page failed to stop, so that it never floods the logs.
/// </summary>
/// <remarks>
/// Kept in <see cref="HubCallerContext.Items"/>: SignalR runs the calls of one connection one at a time, so it needs no
/// lock, and it is gone with the connection.
/// </remarks>
internal sealed class ClientErrorAllowance
{
    /// <summary>
    /// Reports logged per window and per connection: twice the 10 a page sends at most, since a page that comes back
    /// after a long outage sends at once the 20 it queued meanwhile.
    /// </summary>
    public const int ReportsPerWindow = 20;

    /// <summary>The window the reports are counted over.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private static readonly object _key = new();

    private DateTimeOffset _windowStart = DateTimeOffset.MinValue;
    private int _count;
    private bool _dropReported;

    /// <summary>The allowance of the connection of <paramref name="context"/>, created on its first report.</summary>
    public static ClientErrorAllowance Of(HubCallerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Items.TryGetValue(_key, out var allowance))
        {
            return (ClientErrorAllowance)allowance!;
        }

        var created = new ClientErrorAllowance();
        context.Items[_key] = created;
        return created;
    }

    /// <summary>Counts a report received at <paramref name="now"/>, and tells whether to log it.</summary>
    public ClientErrorAdmission Admit(DateTimeOffset now)
    {
        if (now - _windowStart >= Window)
        {
            _windowStart = now;
            _count = 0;
        }

        if (_count < ReportsPerWindow)
        {
            _count++;
            return ClientErrorAdmission.Admitted;
        }

        // Told once per connection: a client that keeps flooding would otherwise log a line a minute, forever.
        if (_dropReported)
        {
            return ClientErrorAdmission.Dropped;
        }

        _dropReported = true;
        return ClientErrorAdmission.FirstDropped;
    }
}
