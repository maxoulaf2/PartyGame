using Microsoft.AspNetCore.StaticFiles;

namespace PartyGame.Server.FrontEnd;

internal static class FrontEndExtensions
{
    private const string PlayerPage = "/";

    // The other pages live in a folder of the web root, so their canonical URL ends with a slash.
    private static readonly string[] _folderPages = ["/display/", "/gm/"];

    /// <summary>
    /// Serves the client built by <c>npm run build</c> from the web root. Pages are always revalidated
    /// so that a new build reaches phones on their next load, while fingerprinted assets are cached for good.
    /// Page URLs tolerate a missing trailing slash and a different case, and a browser opening an unknown
    /// page is sent to the player page: a mistyped address never shows an error.
    /// </summary>
    public static WebApplication UseFrontEnd(this WebApplication app)
    {
        if (!app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
        {
            app.Logger.FrontEndBuildMissing(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"));
        }

        app.Use(RedirectToCanonicalPage);
        // The built-in trailing slash redirect is permanent: browsers would remember it even if the routes change.
        app.UseDefaultFiles(new DefaultFilesOptions { RedirectToAppendTrailingSlash = false });
        app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetCacheControl });
        app.Use(RedirectUnknownPageToPlayerPage);
        return app;
    }

    private static Task RedirectToCanonicalPage(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        if (IsGetOrHead(request))
        {
            var path = request.Path.Value ?? string.Empty;
            foreach (var page in _folderPages)
            {
                var isPage = path.Equals(page, StringComparison.OrdinalIgnoreCase)
                    || path.Equals(page.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                if (isPage && !path.Equals(page, StringComparison.Ordinal))
                {
                    context.Response.Redirect(page + request.QueryString, permanent: false);
                    return Task.CompletedTask;
                }
            }
        }

        return next(context);
    }

    // Runs once static files had their chance, and before endpoints (routing has already matched them).
    private static Task RedirectUnknownPageToPlayerPage(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        var isUnknownPage = context.GetEndpoint() is null
            && IsGetOrHead(request)
            && request.Path != PlayerPage
            && !ServerPaths.IsReserved(request.Path)
            && AcceptsHtml(request)
            // Without a client build the player page is missing too: keep the 404 rather than a redirect to nothing.
            && context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo("index.html").Exists;

        if (isUnknownPage)
        {
            context.Response.Redirect(PlayerPage, permanent: false);
            return Task.CompletedTask;
        }

        return next(context);
    }

    private static bool IsGetOrHead(HttpRequest request) => HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

    // Browsers navigating to a page ask for HTML; scripts and API clients do not.
    private static bool AcceptsHtml(HttpRequest request) =>
        request.GetTypedHeaders().Accept.Any(type => type.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase));

    private static void SetCacheControl(StaticFileResponseContext context)
    {
        var response = context.Context.Response;
        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            response.Headers.CacheControl = "no-cache";
        }
        else if (context.Context.Request.Path.StartsWithSegments(ServerPaths.Assets, StringComparison.Ordinal))
        {
            response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
}
