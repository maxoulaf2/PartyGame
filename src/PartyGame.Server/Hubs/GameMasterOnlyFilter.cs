using Microsoft.AspNetCore.SignalR;
using PartyGame.Contracts;

namespace PartyGame.Server.Hubs;

/// <summary>
/// Ignores the game master intents of a connection that is not authenticated as game master: anybody on the network can
/// open the console. The caller gets no error, only nothing happens.
/// </summary>
internal sealed class GameMasterOnlyFilter(ILogger<GameMasterOnlyFilter> logger) : IHubFilter
{
    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);

        if (invocationContext.HubMethod.IsDefined(typeof(GameMasterOnlyAttribute), inherit: true)
            && invocationContext.Context.GetRole() != Role.GameMaster)
        {
            logger.GameMasterIntentRefused(invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
            return ValueTask.FromResult<object?>(null);
        }

        return next(invocationContext);
    }
}
