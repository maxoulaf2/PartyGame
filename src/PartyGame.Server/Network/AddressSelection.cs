using System.Net;

namespace PartyGame.Server.Network;

/// <summary>
/// The address advertised to phones (encoded in the QR code), computed once at startup.
/// </summary>
/// <param name="Address">The advertised address, or <see langword="null"/> when none was configured nor detected.</param>
/// <param name="IsConfigured">Whether <paramref name="Address"/> comes from <c>Network:AdvertisedAddress</c>.</param>
/// <param name="Candidates">Every detected candidate, best first, whatever the advertised address.</param>
/// <param name="IsOnActiveInterface">Whether an active interface of the host owns <paramref name="Address"/>.</param>
internal sealed record AddressSelection(
    IPAddress? Address,
    bool IsConfigured,
    IReadOnlyList<AddressCandidate> Candidates,
    bool IsOnActiveInterface)
{
    public IEnumerable<AddressCandidate> OtherCandidates => Candidates.Where(candidate => !candidate.Address.Equals(Address));
}
