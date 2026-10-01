using Microsoft.Extensions.Logging;

namespace PartyGame.Server.Tests.Games;

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
