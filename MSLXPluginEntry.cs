using Microsoft.AspNetCore.Mvc.ApplicationParts;
using MSLX.SDK;
using MSLX.SDK.Interfaces;
using Newtonsoft.Json.Linq;
using MSLX.Plugin.DDNS.Core;

[assembly: ApplicationPart("MSLX.Plugin.DDNS")]

namespace MSLX.Plugin.DDNS;

public class MSLXPluginEntry : IPlugin
{
    public static MSLXPluginEntry Instance { get; private set; }
    public string Id => "mslx-plugin-ddns"; 
    public string Name => "DDNS 动态域名解析";
    public string Description => "将本机的 IPv4/IPv6 动态更新到指定的域名解析上，支持 DNSPod、腾讯云、阿里云。";
    public string Version => "1.0.1";
    public string Icon => "jfl_icon.gif";
    public string MinSDKVersion => "1.5.2";
    public string Developer => "xiaoyu";
    public string AuthorUrl => "https://github.com/luluxiaoyu/mslx-plugin-ddns";
    public string PluginUrl => "https://mslx-plugins.mslmc.net/plugins/mslx-plugin-ddns";

    public void OnPluginInitialize(IServiceProvider serviceProvider)
    {
        Instance = this;
        SDK.MSLX.Logger.Info("mslx-plugin-ddns 载入成功~");
    }

    public void OnUnload() {
        SDK.MSLX.Logger.Info("mslx-plugin-ddns 卸载成功~");
    }

    public void OnRegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        // 可以在这里注册高级路由
    }

    public void OnRegisterServices(IServiceCollection services)
    {
        services.AddHostedService<DDNSService>();
    }
}