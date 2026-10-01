using System.Globalization;
using System.Text;
using PartyGame.Server.GameMaster;
using PartyGame.Server.Network;

namespace PartyGame.Server;

/// <summary>
/// What the operator reads in the console to launch the evening. It is written in French, straight to the
/// console rather than through the logger: it is meant for a person, not for the log files.
/// </summary>
internal static class StartupBanner
{
    private const string Rule = "==============================================================";

    public static string Format(AddressSelection selection, int port, GameMasterCode gameMasterCode)
    {
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

        return banner.AppendLine(Rule).ToString();
    }

    private static void Line(StringBuilder banner, string label, string value) =>
        banner.AppendLine(CultureInfo.InvariantCulture, $"  {label,-20}: {value}");
}
