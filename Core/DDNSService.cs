using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MSLX.Plugin.DDNS.Models;
using MSLX.Plugin.DDNS.Core.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MSLX.SDK;

namespace MSLX.Plugin.DDNS.Core;

public class DDNSService : BackgroundService
{
    private static DDNSStatus _status = new DDNSStatus();
    public static DDNSStatus CurrentStatus => _status;

    public static async Task RunSyncAsync()
    {
        _status.IsRunning = true;
        _status.LastRunTime = DateTime.Now;
        _status.IsRunning = true;
        _status.LastRunTime = DateTime.Now;
        DDNSLogger.LogInfo("开始执行 IP 同步检查任务", toSystemLog: false);
        
        try
        {
            var config = ConfigHelper.Load();
            
            IDNSProvider provider = config.Provider switch
            {
                "dnspod" => new DNSPodProvider(config.SecretId, config.SecretKey),
                "tencentcloud" => new TencentCloudProvider(config.SecretId, config.SecretKey),
                "aliyun" => new AliyunProvider(config.SecretId, config.SecretKey),
                "cloudflare" => new CloudflareProvider(config.SecretId, config.SecretKey),
                "huaweicloud" => new HuaweiCloudProvider(config.SecretId, config.SecretKey),
                _ => null
            };

            if (provider == null) throw new Exception("未配置有效的 DNS 服务商");

            // IPv4
            if (config.IPv4.Enable)
            {
                DDNSLogger.LogInfo("正在获取 IPv4 地址...", toSystemLog: false);
                string ip = await GetIP(config.IPv4, System.Net.Sockets.AddressFamily.InterNetwork);
                if (!string.IsNullOrEmpty(ip))
                {
                    if (_status.CurrentIP4 != ip)
                    {
                        DDNSLogger.LogInfo($"检测到 IPv4 地址变化: {_status.CurrentIP4 ?? "无"} -> {ip}，准备更新解析记录", toSystemLog: true);
                        _status.CurrentIP4 = ip;
                        await UpdateDomains(provider, config.IPv4.Domains, ip, "A");
                    }
                    else
                    {
                        DDNSLogger.LogInfo($"IPv4 地址未发生变化 ({ip})，跳过同步", toSystemLog: false);
                    }
                }
                else
                {
                    DDNSLogger.LogError("获取 IPv4 地址失败");
                }
            }

            // IPv6
            if (config.IPv6.Enable)
            {
                DDNSLogger.LogInfo("正在获取 IPv6 地址...", toSystemLog: false);
                string ip = await GetIP(config.IPv6, System.Net.Sockets.AddressFamily.InterNetworkV6);
                if (!string.IsNullOrEmpty(ip))
                {
                    if (_status.CurrentIP6 != ip)
                    {
                        DDNSLogger.LogInfo($"检测到 IPv6 地址变化: {_status.CurrentIP6 ?? "无"} -> {ip}，准备更新解析记录", toSystemLog: true);
                        _status.CurrentIP6 = ip;
                        await UpdateDomains(provider, config.IPv6.Domains, ip, "AAAA");
                    }
                    else
                    {
                        DDNSLogger.LogInfo($"IPv6 地址未发生变化 ({ip})，跳过同步", toSystemLog: false);
                    }
                }
                else
                {
                    DDNSLogger.LogError("获取 IPv6 地址失败");
                }
            }

            _status.LastSuccessTime = DateTime.Now;
            _status.LastErrorMessage = "";
        }
        catch (Exception ex)
        {
            _status.LastErrorMessage = ex.Message;
            DDNSLogger.LogError($"同步异常: {ex.Message}");
        }
        finally
        {
            _status.IsRunning = false;
        }
    }

    private static async Task<string> GetIP(IPConfig config, System.Net.Sockets.AddressFamily family)
    {
        try
        {
            if (config.SourceType == "custom") return config.CustomIP;
            if (config.SourceType == "api") return await IPHelper.GetIPFromApiAsync(config.ApiUrl);
            if (config.SourceType == "nic") return IPHelper.GetIPFromNic(config.NicName, family);
        }
        catch(Exception ex)
        {
            DDNSLogger.LogError($"获取 IP 异常: {ex.Message}");
        }
        return null;
    }

    private static async Task UpdateDomains(IDNSProvider provider, string domainsStr, string ip, string type)
    {
        if (string.IsNullOrEmpty(domainsStr)) return;
        var list = domainsStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var fullDomain in list)
        {
            string clean = fullDomain.Trim();
            var parts = clean.Split('.');
            if (parts.Length < 2) continue;
            
            string subDomain = "tmp";
            string rootDomain = "tmp";
            
            if (clean.StartsWith("@."))
            {
                subDomain = "@";
                rootDomain = clean.Substring(2);
            }
            else
            {
                if (parts.Length == 2)
                {
                    subDomain = "@";
                    rootDomain = clean;
                }
                else
                {
                    subDomain = string.Join(".", parts, 0, parts.Length - 2);
                    rootDomain = string.Join(".", parts, parts.Length - 2, 2);
                }
            }

            await provider.UpdateRecordAsync(rootDomain, subDomain, ip, type);
            DDNSLogger.LogInfo($"更新记录 {subDomain}.{rootDomain} -> {ip}", toSystemLog: true);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var config = ConfigHelper.Load();
            
            if (!string.IsNullOrEmpty(config.SecretId) && !string.IsNullOrEmpty(config.SecretKey))
            {
                await RunSyncAsync();
            }
            
            int interval = config.SyncInterval > 0 ? config.SyncInterval : 5;
            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }
}
