using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace PartyGame.Server.FrontEnd;

/// <summary>
/// The client build the server serves, as identified by the <see cref="FileName"/> that <c>npm run build</c> writes in the
/// web root next to the pages. Read once at startup: every connection is told this identifier, and a page built otherwise
/// reloads itself.
/// </summary>
/// <param name="Id">The identifier of the build, or <see langword="null"/> when the web root holds none.</param>
internal sealed record FrontEndBuild(string? Id)
{
    /// <summary>The file of the web root holding the identifier, as <c>{ "buildId": "..." }</c>.</summary>
    public const string FileName = "build.json";

    /// <summary>
    /// Reads the identifier of the build in <paramref name="webRoot"/>. A missing or unreadable file only means pages are
    /// never told to reload: the server starts all the same.
    /// </summary>
    public static FrontEndBuild Read(IFileProvider webRoot, ILogger logger)
    {
        var file = webRoot.GetFileInfo(FileName);
        if (!file.Exists)
        {
            // Without any page, FrontEndBuildMissing already tells the operator what to do.
            if (webRoot.GetFileInfo("index.html").Exists)
            {
                logger.FrontEndBuildIdMissing(FileName);
            }

            return new FrontEndBuild(Id: null);
        }

        try
        {
            using var stream = file.CreateReadStream();
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("buildId", out var id)
                && id.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(id.GetString()))
            {
                return new FrontEndBuild(id.GetString());
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Unreadable like a file of the wrong shape: logged below.
        }

        logger.FrontEndBuildIdUnreadable(FileName);
        return new FrontEndBuild(Id: null);
    }
}
