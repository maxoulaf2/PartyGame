using System.ComponentModel.DataAnnotations;
using PartyGame.Engine;

namespace PartyGame.Server.Games;

internal sealed class BuzzerOptions
{
    public const string SectionName = "Buzzer";

    // How long the buzzer waits after the first buzz before it designates the winner, handed to the engine.
    [Range(0, 1000)]
    public int ArbitrationMilliseconds { get; init; } = (int)GameContext.DefaultBuzzerArbitrationWindow.TotalMilliseconds;
}
