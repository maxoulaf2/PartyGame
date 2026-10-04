namespace PartyGame.Server.Faults;

internal sealed class FaultInjectionOptions
{
    public const string SectionName = "FaultInjection";

    public const string FailOnInputSetting = $"{SectionName}:{nameof(FailOnInput)}";

    // Name of the type of input whose handling throws, such as PlayerRoundInput. Nothing is injected when empty.
    public string? FailOnInput { get; init; }

    // How many inputs of that type throw, after which the engine handles them again.
    public int FailCount { get; init; } = 1;

    public bool IsRequested => !string.IsNullOrWhiteSpace(FailOnInput);

    public static FaultInjectionOptions Read(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<FaultInjectionOptions>() ?? new FaultInjectionOptions();
}
