namespace PartyGame.Server;

/// <summary>
/// URL prefixes owned by the server rather than by the pages: a request under one of them is a technical call,
/// which must get a plain 404 when nothing answers it, never a redirect to an HTML page.
/// </summary>
internal static class ServerPaths
{
    public static readonly PathString Api = "/api";
    public static readonly PathString Hub = "/hub";
    public static readonly PathString Media = "/media";

    /// <summary>Where Vite emits the fingerprinted scripts, styles and fonts of the client build.</summary>
    public static readonly PathString Assets = "/assets";

    public static readonly PathString Health = "/health";

    public static readonly IReadOnlyList<PathString> Reserved = [Api, Hub, Media, Assets, Health];

    public static bool IsReserved(PathString path) =>
        Reserved.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
