using PartyGame.Engine.Inputs;
using PartyGame.Engine.State;

namespace PartyGame.Engine.Lobby;

/// <summary>
/// Choice by the game master of the address encoded in the QR code. Allowed in every phase, since registration stays
/// open; phones already registered keep the address they came through.
/// </summary>
internal static class AddressChoice
{
    public static Transition Choose(GameState state, ChooseAdvertisedAddress choice)
    {
        // Only an address the server detected (or was configured with) can be chosen: the console cannot be used to send
        // the phones of the room to an arbitrary host.
        if (!state.JoinAddressCandidates.Any(candidate => string.Equals(candidate.Address, choice.Address, StringComparison.Ordinal)))
        {
            return Transition.Rejected(state, RejectionReason.AddressUnknown);
        }

        if (string.Equals(state.JoinAddress, choice.Address, StringComparison.Ordinal))
        {
            return Transition.Unchanged(state);
        }

        return new Transition(state with { JoinAddress = choice.Address }, []);
    }
}
