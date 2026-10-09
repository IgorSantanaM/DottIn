using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace DottIn.Presentation.WebApi.Security;

public static class ReverseProxyConfiguration
{
    public static void Configure(
        ForwardedHeadersOptions options,
        IConfiguration configuration,
        IEnumerable<IPNetwork>? containerNetworks = null)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 2;
        foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            options.KnownIPNetworks.Add(IPNetwork.Parse(network));

        if (!configuration.GetValue<bool>("ReverseProxy:TrustContainerNetwork"))
            return;

        // Opt-in for the isolated Compose bridge only; never clear the proxy allowlist.
        var networks = (containerNetworks ?? DiscoverContainerNetworks()).Distinct().ToArray();
        if (networks.Length == 0)
            throw new InvalidOperationException("No private container network found for reverse proxy trust.");
        foreach (var network in networks)
            options.KnownIPNetworks.Add(network);
    }

    public static IPNetwork? GetPrivateNetwork(IPAddress address, int prefixLength)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || prefixLength is < 0 or > 32)
            return null;

        var bytes = address.GetAddressBytes();
        var minimumPrefix = bytes[0] switch
        {
            10 => 8,
            172 when bytes[1] is >= 16 and <= 31 => 12,
            192 when bytes[1] == 168 => 16,
            _ => 33
        };
        if (prefixLength < minimumPrefix)
            return null;

        for (var index = 0; index < bytes.Length; index++)
        {
            var bits = Math.Clamp(prefixLength - index * 8, 0, 8);
            bytes[index] &= (byte)(0xff << (8 - bits));
        }
        return new IPNetwork(new IPAddress(bytes), prefixLength);
    }

    private static IEnumerable<IPNetwork> DiscoverContainerNetworks()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
                "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ReverseProxy:TrustContainerNetwork is only supported inside a container.");

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                var network = GetPrivateNetwork(address.Address, address.PrefixLength);
                if (network.HasValue)
                    yield return network.Value;
            }
        }
    }
}
