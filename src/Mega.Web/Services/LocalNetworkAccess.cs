using System.Net;

namespace Mega.Web.Services;

// Remote access requires an explicit RFC1918 subnet; the default remains loopback only.
public sealed class LocalNetworkAccess
{
    private readonly IPNetwork? network;
    public LocalNetworkAccess(string? subnet)
    {
        if (string.IsNullOrWhiteSpace(subnet)) return;
        if (!IPNetwork.TryParse(subnet, out var parsed) ||
            !new[] { IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("172.16.0.0/12"), IPNetwork.Parse("192.168.0.0/16") }
                .Any(privateNetwork => parsed.PrefixLength >= privateNetwork.PrefixLength && privateNetwork.Contains(parsed.BaseAddress)))
            throw new InvalidOperationException("MEGA:AllowedNetwork debe ser una subred IPv4 privada en formato CIDR.");
        network = parsed;
    }
    public bool Allows(IPAddress remote)
    {
        if (remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        return IPAddress.IsLoopback(remote) || network?.Contains(remote) == true;
    }
}
