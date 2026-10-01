using Microsoft.Extensions.Options;

namespace PartyGame.Server.GameMaster;

internal static class GameMasterExtensions
{
    /// <summary>
    /// Provides the <see cref="GameMasterCode"/> of this run. A configured code with the wrong format stops the server
    /// at startup, rather than leaving the game master locked out in the middle of the evening.
    /// </summary>
    public static WebApplicationBuilder AddGameMasterCode(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<GameMasterOptions>()
            .BindConfiguration(GameMasterOptions.SectionName)
            .Validate(
                options => string.IsNullOrWhiteSpace(options.Code) || GameMasterOptions.IsValidCode(options.Code),
                $"{GameMasterOptions.CodeSetting} must be made of exactly {GameMasterCode.Length} digits, such as 123456, or be left empty to generate a code at each start")
            .ValidateOnStart();

        builder.Services.AddSingleton(services => GameMasterCode.Create(services.GetRequiredService<IOptions<GameMasterOptions>>().Value));

        return builder;
    }
}
