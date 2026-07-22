using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.Http;

namespace MSLX.Plugin.DDNS.Core;

public static class IPHelper
{
    public static async Task<string> GetIPFromApiAsync(string url)
    {
        using var client = new HttpClient();
        var resp = await client.GetStringAsync(url);
        return resp.Trim();
    }

    public static string GetIPFromNic(string nicName, AddressFamily family)
    {
        // 去除附加的 IP 信息，例如 "Ethernet0 (192.168...)" -> "Ethernet0"
        var actualNicName = nicName;
        int idx = nicName.IndexOf(" (");
        if (idx > 0)
        {
            actualNicName = nicName.Substring(0, idx).Trim();
        }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.Name == actualNicName || nic.Description == actualNicName)
            {
                var props = nic.GetIPProperties();
                foreach (var ip in props.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == family && !IPAddress.IsLoopback(ip.Address))
                    {
                        if (family == AddressFamily.InterNetworkV6 && ip.Address.IsIPv6LinkLocal) continue;
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return null;
    }

    public static List<string> GetNicNames()
    {
        var list = new List<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                var ips = new List<string>();
                foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (!IPAddress.IsLoopback(ip.Address))
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 && ip.Address.IsIPv6LinkLocal) continue;
                        ips.Add(ip.Address.ToString());
                    }
                }
                
                if (ips.Count > 0)
                {
                    list.Add($"{nic.Name} ({string.Join(", ", ips)})");
                }
            }
        }
        return list;
    }
}
