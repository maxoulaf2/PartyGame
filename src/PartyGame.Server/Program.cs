using System.Globalization;
using PartyGame.Server.FrontEnd;
using PartyGame.Server.Logging;
using Serilog;

// Catches failures that happen before the configuration is available (e.g. unreadable appsettings).
using var bootstrapLogger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateLogger();
IConfiguration? configuration = null;

try
{
    var builder = WebApplication.CreateBuilder(args);
    configuration = builder.Configuration;

    builder.Services.AddSerilog(ServerLogging.CreateLogger(configuration), dispose: true);

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseFrontEnd();
    app.MapHealthChecks("/health");

    app.Logger.ServerStarting(app.Environment.EnvironmentName);

    app.Run();
    return 0;
}
catch (HostAbortedException)
{
    // Design-time tools (e.g. dotnet ef) abort the entry point once the host is built: not a failure.
    throw;
}
catch (Exception ex)
{
    // A failing host disposes its logger before the exception gets here, so the fatal error needs a logger of its own.
    // Disposing it flushes the message before the process exits.
    using var fatalLogger = configuration is null ? null : ServerLogging.CreateLogger(configuration);
    ((Serilog.ILogger?)fatalLogger ?? bootstrapLogger).Fatal(ex, "Server terminated unexpectedly");
    return 1;
}
