namespace PartyGame.Contracts;

/// <summary>
/// How a page reaches the hub. WebSockets is the one expected on a local network: a fallback means that something between
/// the phone and the server, such as a proxy or a power saving mode, blocks it, and every message then comes later.
/// </summary>
public enum ConnectionTransport
{
    /// <summary>A WebSocket: messages go both ways at once.</summary>
    WebSockets,

    /// <summary>A fallback: the server streams its messages, each message of the page is a request of its own.</summary>
    ServerSentEvents,

    /// <summary>The slowest fallback: every message waits for a request of the page.</summary>
    LongPolling,
}
