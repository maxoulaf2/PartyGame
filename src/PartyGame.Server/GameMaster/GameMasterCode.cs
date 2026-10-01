using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PartyGame.Server.GameMaster;

/// <summary>
/// The secret that lets the game master drive the game, since anybody on the network can open <c>/gm/</c>.
/// It is checked here and never handed out: only the startup banner reveals it, on the console, out of the log files.
/// </summary>
internal sealed class GameMasterCode
{
    public const int Length = 6;

    private readonly byte[] _code;

    private GameMasterCode(string code, bool isConfigured)
    {
        _code = Encoding.ASCII.GetBytes(code);
        IsConfigured = isConfigured;
    }

    /// <summary>Whether the code comes from <see cref="GameMasterOptions.CodeSetting"/> rather than from the generator.</summary>
    public bool IsConfigured { get; }

    /// <summary>Uses the configured code, already validated, or generates a new one.</summary>
    public static GameMasterCode Create(GameMasterOptions options) =>
        string.IsNullOrWhiteSpace(options.Code)
            ? new GameMasterCode(Generate(), isConfigured: false)
            : new GameMasterCode(options.Code.Trim(), isConfigured: true);

    /// <summary>
    /// Checks a code typed by the game master. Surrounding spaces are tolerated, as phone keyboards add them.
    /// The comparison takes the same time whatever the number of matching digits, so timing does not leak the code.
    /// </summary>
    public bool Verify(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(_code, Encoding.UTF8.GetBytes(candidate.Trim()));
    }

    /// <summary>The code itself, for the startup banner only.</summary>
    public string RevealForBanner() => Encoding.ASCII.GetString(_code);

    private static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
}
