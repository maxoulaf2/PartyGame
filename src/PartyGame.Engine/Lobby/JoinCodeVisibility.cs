using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// The game master shows or hides the QR code on the TV screen, which the lobby always shows. Allowed in every phase, since
/// registration stays open: the TV screen shows it over the game without changing its course.
/// </summary>
internal static class JoinCodeVisibility
{
    public static Transition Show(GameState state, ShowJoinCode show) =>
        state.JoinCodeShown == show.Shown
            ? Transition.Unchanged(state)
            : new Transition(state with { JoinCodeShown = show.Shown }, []);
}
