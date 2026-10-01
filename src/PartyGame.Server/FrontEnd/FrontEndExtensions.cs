using Microsoft.AspNetCore.StaticFiles;

namespace PartyGame.Server.FrontEnd;

internal static class FrontEndExtensions
{
    // Vite fingerprints everything it emits under /assets/: a new build gets new URLs.
    private const string FingerprintedAssetsPath = "/assets";

    /// <summary>
    /// Serves the client built by <c>npm run build</c> from the web root. Pages are always revalidated
    /// so that a new build reaches phones on their next load, while fingerprinted assets are cached for good.
    /// </summary>
    public static WebApplication UseFrontEnd(this WebApplication app)
    {
        if (!app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
        {
            app.Logger.FrontEndBuildMissing(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"));
        }

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetCacheControl });
        return app;
    }

    private static void SetCacheControl(StaticFileResponseContext context)
    {
        var response = context.Context.Response;
        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            response.Headers.CacheControl = "no-cache";
        }
        else if (context.Context.Request.Path.StartsWithSegments(FingerprintedAssetsPath, StringComparison.Ordinal))
        {
            response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
}
