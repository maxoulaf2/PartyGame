using System.Globalization;
using Microsoft.AspNetCore.Connections;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PartyGame.Server.Logging;

internal static class ServerLogging
{
    public const string FileNamePattern = "partygame-.log";

    public static Logger CreateLogger(IConfiguration configuration)
    {
        var options = configuration.GetSection(LogFileOptions.SectionName).Get<LogFileOptions>() ?? new LogFileOptions();
        var directory = Path.Combine(AppContext.BaseDirectory, options.Directory);

        // The file sink opens its file lazily and Serilog isolates sink failures,
        // so an unwritable log folder degrades to console-only logging instead of blocking startup.
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Filter.ByExcluding(IsPortInUseStackTrace)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(directory, FileNamePattern),
                fileSizeLimitBytes: options.FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCount)
            .CreateLogger();
    }

    public static bool IsPortInUse(Exception? exception) =>
        exception is IOException { InnerException: AddressInUseException };

    // The host logs a busy port with its full stack trace before rethrowing. The entry point reports it
    // instead with a plain message telling the operator what to do.
    private static bool IsPortInUseStackTrace(LogEvent logEvent) =>
        IsPortInUse(logEvent.Exception)
        && logEvent.Properties.TryGetValue("SourceContext", out var source)
        && source is ScalarValue { Value: string context }
        && context.StartsWith("Microsoft.Extensions.Hosting", StringComparison.Ordinal);
}
