using Microsoft.AspNetCore.SignalR;

namespace PartyGame.Server.Hubs;

/// <summary>
/// Keeps the exceptions of hub methods on the server: a hub method that throws is a bug, logged as an error, and the
/// client only gets an empty answer. SignalR alone would answer a generic error and log it below the visible levels.
/// </summary>
internal sealed class HubExceptionFilter(ILogger<HubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            return await next(invocationContext).ConfigureAwait(false);
        }
        catch (Exception ex) when (!invocationContext.Context.ConnectionAborted.IsCancellationRequested)
        {
            logger.HubMethodFailed(ex, invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
            return null;
        }
    }
}
