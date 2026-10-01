namespace PartyGame.Server.GameMaster;

internal sealed class GameMasterOptions
{
    public const string SectionName = "GameMaster";

    public const string CodeSetting = $"{SectionName}:{nameof(Code)}";

    // Fixed code for development and E2E tests. When empty, a new code is generated at each start.
    public string? Code { get; init; }

    public static bool IsValidCode(string? code) =>
        code is not null && code.Trim() is { Length: GameMasterCode.Length } trimmed && trimmed.All(char.IsAsciiDigit);
}
