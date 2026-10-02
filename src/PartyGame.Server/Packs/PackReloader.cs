using PartyGame.Engine.Inputs;
using PartyGame.Server.Games;

namespace PartyGame.Server.Packs;

/// <summary>
/// Reloads the packs at the request of the game master. The files are read here, outside the loop, which only gets their
/// result: the engine never waits for the disk.
/// </summary>
internal sealed class PackReloader(PackLibraryLoader loader, IGameInputWriter inputs) : IDisposable
{
    // One reload at a time: two consoles reloading at once must not hand the loop an older reading after a newer one.
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Loads and checks the packs again, then hands them to the loop, which refuses them once the game is started.
    /// </summary>
    /// <returns>What the loop made of the new catalog.</returns>
    public async Task<InputOutcome> ReloadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var library = loader.Load();
            return await inputs.SubmitAsync(new PacksLoaded(library.ToCatalog()), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
