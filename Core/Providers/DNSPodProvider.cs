using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;

namespace MSLX.Plugin.DDNS.Core.Providers;

public class DNSPodProvider : IDNSProvider
{
    private readonly string _token;
    
    public DNSPodProvider(string secretId, string secretKey)
    {
        _token = $"{secretId},{secretKey}";
    }

    public async Task UpdateRecordAsync(string domain, string subDomain, string ip, string type)
    {
        var client = new HttpClient();
        
        // 1. 获取记录列表
        var listData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("login_token", _token),
            new KeyValuePair<string, string>("format", "json"),
            new KeyValuePair<string, string>("domain", domain),
            new KeyValuePair<string, string>("sub_domain", subDomain)
        });
        
        var listRes = await client.PostAsync("https://dnsapi.cn/Record.List", listData);
        var listJson = JObject.Parse(await listRes.Content.ReadAsStringAsync());
        
        if (listJson["status"]?["code"]?.ToString() != "1")
        {
            // 如果不存在该记录，尝试创建
            var createData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login_token", _token),
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("domain", domain),
                new KeyValuePair<string, string>("sub_domain", subDomain),
                new KeyValuePair<string, string>("record_type", type),
                new KeyValuePair<string, string>("record_line", "默认"),
                new KeyValuePair<string, string>("value", ip)
            });
            var createRes = await client.PostAsync("https://dnsapi.cn/Record.Create", createData);
            var createJson = JObject.Parse(await createRes.Content.ReadAsStringAsync());
            if (createJson["status"]?["code"]?.ToString() != "1")
                throw new Exception($"DNSPod创建记录失败: {createJson["status"]?["message"]}");
            return;
        }
        
        var records = listJson["records"] as JArray;
        if (records == null || records.Count == 0) throw new Exception("DNSPod 未找到且无法创建记录");
        
        var recordId = records[0]["id"]?.ToString();
        var recordValue = records[0]["value"]?.ToString();
        
        if (recordValue == ip) return; // IP没有变化
        
        // 2. 修改记录
        var modifyData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("login_token", _token),
            new KeyValuePair<string, string>("format", "json"),
            new KeyValuePair<string, string>("domain", domain),
            new KeyValuePair<string, string>("record_id", recordId),
            new KeyValuePair<string, string>("sub_domain", subDomain),
            new KeyValuePair<string, string>("record_type", type),
            new KeyValuePair<string, string>("record_line", "默认"),
            new KeyValuePair<string, string>("value", ip)
        });
        
        var modRes = await client.PostAsync("https://dnsapi.cn/Record.Modify", modifyData);
        var modJson = JObject.Parse(await modRes.Content.ReadAsStringAsync());
        if (modJson["status"]?["code"]?.ToString() != "1")
        {
            throw new Exception($"DNSPod更新失败: {modJson["status"]?["message"]}");
        }
    }
}
