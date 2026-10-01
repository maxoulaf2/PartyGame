using System.Globalization;
using Serilog;
using Serilog.Core;
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
}
