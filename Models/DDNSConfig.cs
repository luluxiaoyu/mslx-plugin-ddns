using System.ComponentModel.DataAnnotations;

namespace MSLX.Plugin.DDNS.Models;

public class DDNSConfig
{
    [Required]
    [AllowedValues("dnspod", "tencentcloud", "aliyun", "cloudflare", "huaweicloud", ErrorMessage = "不支持的 DNS 服务商")]
    public string Provider { get; set; } = "dnspod";
    
    public string SecretId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    
    [Range(1, 1440, ErrorMessage = "同步间隔必须在 1 到 1440 分钟之间")]
    public int SyncInterval { get; set; } = 5;

    public IPConfig IPv4 { get; set; } = new IPConfig { SourceType = "api", ApiUrl = "https://api.ipify.org" };
    public IPConfig IPv6 { get; set; } = new IPConfig { SourceType = "api", ApiUrl = "https://api6.ipify.org" };
}

public class IPConfig
{
    public bool Enable { get; set; } = false;
    
    [Required]
    [AllowedValues("custom", "api", "nic", ErrorMessage = "不支持的地址获取方式")]
    public string SourceType { get; set; } = "api"; // "custom", "api", "nic"
    
    public string CustomIP { get; set; } = "";
    public string ApiUrl { get; set; } = "";
    public string NicName { get; set; } = "";
    public string Domains { get; set; } = ""; // 逗号分隔的域名列表，例如: example.com,@.example.com
}
