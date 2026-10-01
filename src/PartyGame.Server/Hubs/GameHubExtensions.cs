using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts.Serialization;

namespace PartyGame.Server.Hubs;

internal static class GameHubExtensions
{
    /// <summary>
    /// Adds SignalR with the wire conventions of <see cref="ContractJsonOptions"/>, and the filters every hub method goes
    /// through.
    /// </summary>
    public static WebApplicationBuilder AddGameHub(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddSignalR(options =>
            {
                // Exception details reach the browser console only while developing.
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();

                // Outermost first: an exception of the game master check is caught too.
                options.AddFilter<HubExceptionFilter>();
                options.AddFilter<GameMasterOnlyFilter>();
            })
            .AddJsonProtocol(options => ContractJsonOptions.Apply(options.PayloadSerializerOptions));

        return builder;
    }

    public static WebApplication MapGameHub(this WebApplication app)
    {
        app.MapHub<GameHub>(ServerPaths.GameHub);
        return app;
    }
}
