using System.Globalization;
using System.Text;
using PartyGame.Content;
using PartyGame.Contracts.Packs;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Network;
using PartyGame.Server.Packs;

namespace PartyGame.Server;

/// <summary>
/// What the operator reads in the console to launch the evening. It is written in French, straight to the
/// console rather than through the logger: it is meant for a person, not for the log files.
/// </summary>
internal static class StartupBanner
{
    private const string Rule = "==============================================================";

    public static string Format(AddressSelection selection, int port, GameMasterCode gameMasterCode, PackLibrary packs, bool gamePending = false)
    {
        ArgumentNullException.ThrowIfNull(packs);

        var host = selection.Address?.ToString() ?? "localhost";
        var banner = new StringBuilder()
            .AppendLine()
            .AppendLine(Rule)
            .AppendLine("  PartyGame est prêt")
            .AppendLine();

        if (selection.Address is null)
        {
            banner
                .AppendLine("  Aucune adresse de réseau local trouvée : les téléphones ne pourront")
                .AppendLine("  pas rejoindre la partie. Connectez ce PC au Wi-Fi, puis relancez le serveur.")
                .AppendLine();
        }
        else
        {
            var origin = selection.IsConfigured ? $"imposée par {NetworkExtensions.AdvertisedAddressSetting}" : "détectée";
            Line(banner, "Adresse des joueurs", $"{selection.Address} ({origin})");
            if (!selection.IsOnActiveInterface)
            {
                banner.AppendLine("  Attention : aucune interface réseau active ne porte cette adresse.");
            }
        }

        Line(banner, "Écran TV", $"http://{host}:{port}/display/");
        Line(banner, "Game master", $"http://{host}:{port}/gm/");
        var codeOrigin = gameMasterCode.IsConfigured ? $" (imposé par {GameMasterOptions.CodeSetting})" : string.Empty;
        Line(banner, "Code game master", gameMasterCode.RevealForBanner() + codeOrigin);

        var others = selection.OtherCandidates.ToList();
        if (others.Count > 0)
        {
            banner.AppendLine().AppendLine(selection.IsConfigured ? "  Adresses détectées :" : "  Autres adresses possibles :");
            foreach (var candidate in others)
            {
                banner.AppendLine(CultureInfo.InvariantCulture, $"    {candidate.Address} ({candidate.InterfaceName})");
            }

            var example = others[0].Address;
            banner
                .AppendLine("  Pour en imposer une, relancez avec par exemple :")
                .AppendLine(CultureInfo.InvariantCulture, $"    --{NetworkExtensions.AdvertisedAddressSetting}={example}")
                .AppendLine(CultureInfo.InvariantCulture, $"    ou la variable d'environnement {NetworkExtensions.AdvertisedAddressSetting.Replace(":", "__", StringComparison.Ordinal)}={example}");
        }

        if (gamePending)
        {
            banner
                .AppendLine()
                .AppendLine("  Partie interrompue trouvée : elle attend votre décision dans la console")
                .AppendLine("  du game master (reprendre la partie ou en commencer une nouvelle).");
        }

        AppendPacks(banner, packs);

        return banner.AppendLine(Rule).ToString();
    }

    private static void AppendPacks(StringBuilder banner, PackLibrary packs)
    {
        banner.AppendLine();
        if (!packs.DirectoryExists)
        {
            banner
                .AppendLine(CultureInfo.InvariantCulture, $"  Aucun pack : le dossier {packs.Directory} est introuvable.")
                .AppendLine("  Pour indiquer le dossier des packs, relancez avec :")
                .AppendLine(CultureInfo.InvariantCulture, $"    --{PacksOptions.DirectorySetting}=<chemin du dossier des packs>");
            return;
        }

        if (packs.Packs.IsEmpty)
        {
            banner
                .AppendLine(CultureInfo.InvariantCulture, $"  Aucun pack dans le dossier {packs.Directory}.")
                .AppendLine(CultureInfo.InvariantCulture, $"  Un pack est un sous-dossier qui contient un fichier {PackDescriptor.FileName}.");
            return;
        }

        banner.AppendLine(CultureInfo.InvariantCulture, $"  Packs ({packs.Directory}) :");
        foreach (var pack in packs.Packs)
        {
            var title = pack.Title is null ? "sans titre" : $"« {pack.Title} »";
            var rounds = pack.RoundCount is { } count ? $", {Plural(count, "manche")}" : string.Empty;
            var state = pack.IsValid ? "valide" : $"invalide ({Plural(pack.Problems.Length, "problème")})";
            banner.AppendLine(CultureInfo.InvariantCulture, $"    {pack.Id,-20}: {title}{rounds}, {state}");
        }

        if (packs.Packs.Any(pack => !pack.IsValid))
        {
            banner.AppendLine("  Les problèmes des packs invalides sont détaillés dans le journal ci-dessus.");
        }
    }

    private static string Plural(int count, string noun) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {noun}{(count > 1 ? "s" : string.Empty)}");

    private static void Line(StringBuilder banner, string label, string value) =>
        banner.AppendLine(CultureInfo.InvariantCulture, $"  {label,-20}: {value}");
}
