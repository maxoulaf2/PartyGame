using System.Collections.Immutable;
using System.Net;
using PartyGame.Engine.State;

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

    /// <summary>
    /// The addresses the game master may advertise instead: every candidate, preceded by the configured address when no
    /// candidate holds it, so that it can be chosen back after another one.
    /// </summary>
    public ImmutableArray<JoinAddressCandidate> ToJoinAddressCandidates()
    {
        var candidates = Candidates.Select(candidate => new JoinAddressCandidate(candidate.Address.ToString(), candidate.InterfaceName));
        return Address is not null && !Candidates.Any(candidate => candidate.Address.Equals(Address))
            ? [new JoinAddressCandidate(Address.ToString(), InterfaceName: null), .. candidates]
            : [.. candidates];
    }
}
