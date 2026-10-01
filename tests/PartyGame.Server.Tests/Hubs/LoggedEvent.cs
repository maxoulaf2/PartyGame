using System.Text.Json;

namespace PartyGame.Server.Tests.Hubs;

/// <summary>
/// An event of the log file, written by Serilog in compact JSON.
/// </summary>
/// <param name="Level">Serilog level; the compact format omits it for <c>Information</c>.</param>
/// <param name="Template">The message template, such as <c>Connection {ConnectionId} announced as {Role}</c>.</param>
/// <param name="Line">The whole line, with every property.</param>
internal sealed record LoggedEvent(string Level, string Template, string Line)
{
    public static IReadOnlyList<LoggedEvent> ReadAll(TempDirectory logs) =>
        [.. logs.ReadAllLogs()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Parse)];

    private static LoggedEvent Parse(string line)
    {
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        var level = root.TryGetProperty("@l", out var l) ? l.GetString()! : "Information";
        return new LoggedEvent(level, root.GetProperty("@mt").GetString()!, line);
    }
}
