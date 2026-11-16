using System.Net;
using Mega.Web.Services;

namespace Mega.Tests;

public sealed class LocalNetworkAccessTests
{
    [Theory]
    [InlineData(null, "127.0.0.1", true)]
    [InlineData(null, "192.168.0.25", false)]
    [InlineData("192.168.0.0/24", "192.168.0.25", true)]
    [InlineData("192.168.0.0/24", "::ffff:192.168.0.25", true)]
    [InlineData("192.168.0.0/24", "192.168.1.25", false)]
    [InlineData("192.168.0.0/24", "8.8.8.8", false)]
    public void RestrictsAccessToExplicitSubnet(string? subnet, string address, bool allowed)
        => Assert.Equal(allowed, new LocalNetworkAccess(subnet).Allows(IPAddress.Parse(address)));

    [Theory]
    [InlineData("0.0.0.0/0")]
    [InlineData("8.8.8.0/24")]
    [InlineData("192.168.0.0/15")]
    [InlineData("invalid")]
    public void RejectsPublicOrInvalidNetworks(string subnet)
        => Assert.Throws<InvalidOperationException>(() => new LocalNetworkAccess(subnet));
}
