using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MSLX.SDK;
using MSLX.Plugin.DDNS.Models;

namespace MSLX.Plugin.DDNS.Core;

public static class ConfigHelper
{
    public static DDNSConfig Load()
    {
        var _cfg = MSLXPluginEntry.Instance.Config();
        var config = new DDNSConfig();
        
        var provider = _cfg.ReadConfigKey("Provider");
        if (provider != null) config.Provider = provider.ToString();
        
        var sid = _cfg.ReadConfigKey("SecretId")?.ToString();
        config.SecretId = string.IsNullOrEmpty(sid) ? "" : CryptoHelper.Decrypt(sid);
        
        var skey = _cfg.ReadConfigKey("SecretKey")?.ToString();
        config.SecretKey = string.IsNullOrEmpty(skey) ? "" : CryptoHelper.Decrypt(skey);
        
        var syncInterval = _cfg.ReadConfigKey("SyncInterval");
        if (syncInterval != null && int.TryParse(syncInterval.ToString(), out int si)) 
        {
            config.SyncInterval = si;
        }
            
        var v4 = _cfg.ReadConfigKey("IPv4");
        if (v4 != null) 
        {
            try { config.IPv4 = JsonConvert.DeserializeObject<IPConfig>(v4.ToString()); } catch {}
        }
        
        var v6 = _cfg.ReadConfigKey("IPv6");
        if (v6 != null) 
        {
            try { config.IPv6 = JsonConvert.DeserializeObject<IPConfig>(v6.ToString()); } catch {}
        }
        
        return config;
    }

    public static void Save(DDNSConfig config)
    {
        var _cfg = MSLXPluginEntry.Instance.Config();
        
        _cfg.WriteConfigKey("Provider", config.Provider);
        _cfg.WriteConfigKey("SecretId", CryptoHelper.Encrypt(config.SecretId));
        _cfg.WriteConfigKey("SecretKey", CryptoHelper.Encrypt(config.SecretKey));
        _cfg.WriteConfigKey("SyncInterval", config.SyncInterval);
        _cfg.WriteConfigKey("IPv4", JObject.FromObject(config.IPv4));
        _cfg.WriteConfigKey("IPv6", JObject.FromObject(config.IPv6));
    }
}
