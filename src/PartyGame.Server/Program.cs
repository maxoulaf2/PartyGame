using System.Globalization;
using Microsoft.Extensions.Options;
using PartyGame.Contracts.Serialization;
using PartyGame.Server;
using PartyGame.Server.FrontEnd;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Logging;
using PartyGame.Server.Network;
using Serilog;

// Catches failures that happen before the configuration is available (e.g. unreadable appsettings).
using var bootstrapLogger = new LoggerConfiguration().WriteTo.Console(formatProvider: CultureInfo.InvariantCulture).CreateLogger();
IConfiguration? configuration = null;

try
{
    var builder = WebApplication.CreateBuilder(args);
    configuration = builder.Configuration;

    builder.Services.AddSerilog(ServerLogging.CreateLogger(configuration), dispose: true);
    builder.AddLocalNetworkListening();
    builder.AddGameMasterCode();

    builder.Services.ConfigureHttpJsonOptions(options => ContractJsonOptions.Apply(options.SerializerOptions));
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseFrontEnd();
    app.MapHealthChecks(ServerPaths.Health);
    app.MapJoinInfo();

    app.Logger.ServerStarting(app.Environment.EnvironmentName);
    app.Lifetime.ApplicationStarted.Register(() => Console.Out.Write(StartupBanner.Format(
        app.Services.GetRequiredService<AddressSelection>(),
        app.Services.GetRequiredService<IOptions<NetworkOptions>>().Value.Port,
        app.Services.GetRequiredService<GameMasterCode>())));

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
    var logger = (Serilog.ILogger?)fatalLogger ?? bootstrapLogger;
    if (ServerLogging.IsPortInUse(ex) && configuration is not null)
    {
        // A common operator mistake (a second server, another app on the port): no stack trace, just what to do.
        logger.Fatal(
            "Port {Port} is already in use by another program. Stop that program, or choose another port with the {Setting} setting (e.g. environment variable Network__Port=5001 or argument --Network:Port=5001)",
            NetworkOptions.Read(configuration).Port,
            $"{NetworkOptions.SectionName}:Port");
    }
    else
    {
        logger.Fatal(ex, "Server terminated unexpectedly");
    }

    return 1;
}
