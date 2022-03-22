using System;
using System.Net;
using System.Net.Sockets;

namespace NServiceBus.Extensions.Diagnostics;

internal static class IpAddressResolver
{
    private static readonly Lazy<string?> _ipAddressResolver = new(GetIpAddress);

    public static string? Value => _ipAddressResolver.Value;

    private static string? GetIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endpoint)
            {
                return endpoint.Address.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

}