using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MSLX.Plugin.DDNS.Core.Providers;

public class CloudflareProvider : IDNSProvider
{
    private readonly string _secretId;
    private readonly string _secretKey;
    private static readonly HttpClient HttpClientInstance = new HttpClient();

    public CloudflareProvider(string secretId, string secretKey)
    {
        _secretId = secretId;
        _secretKey = secretKey;
    }

    public async Task UpdateRecordAsync(string domain, string subDomain, string ip, string type)
    {
        // 1. Get Zone ID
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones?name={domain}");
        AddAuthHeaders(requestMessage, _secretId, _secretKey);

        var response = await HttpClientInstance.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();

        string jsonStr = await response.Content.ReadAsStringAsync();
        var jobj = JObject.Parse(jsonStr);

        var zones = jobj["result"] as JArray;
        if (zones == null || zones.Count == 0)
        {
            throw new Exception($"Cloudflare 未找到域名 {domain} 的 Zone 记录");
        }

        string zoneId = zones[0]["id"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(zoneId))
        {
            throw new Exception("Cloudflare 返回的 Zone ID 为空");
        }

        // 2. Find existing record
        string fullDomain = subDomain == "@" ? domain : $"{subDomain}.{domain}";
        var getRecordRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records?type={type}&name={fullDomain}");
        AddAuthHeaders(getRecordRequest, _secretId, _secretKey);

        var recordResponse = await HttpClientInstance.SendAsync(getRecordRequest);
        recordResponse.EnsureSuccessStatusCode();

        string recordJsonStr = await recordResponse.Content.ReadAsStringAsync();
        var recordJobj = JObject.Parse(recordJsonStr);
        var records = recordJobj["result"] as JArray;

        string recordId = null;
        string recordValue = null;

        if (records != null && records.Count > 0)
        {
            recordId = records[0]["id"]?.ToString();
            recordValue = records[0]["content"]?.ToString();
        }

        // 3. Update or Create
        if (string.IsNullOrEmpty(recordId))
        {
            // Create
            var postData = new JObject
            {
                ["type"] = type,
                ["name"] = fullDomain,
                ["content"] = ip,
                ["ttl"] = 120,
                ["proxied"] = false
            };

            var postRequest = new HttpRequestMessage(HttpMethod.Post, $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records");
            AddAuthHeaders(postRequest, _secretId, _secretKey);
            postRequest.Content = new StringContent(postData.ToString(), Encoding.UTF8, "application/json");

            var postResponse = await HttpClientInstance.SendAsync(postRequest);
            if (!postResponse.IsSuccessStatusCode)
            {
                string err = await postResponse.Content.ReadAsStringAsync();
                throw new Exception($"Cloudflare 添加记录失败: {err}");
            }
        }
        else if (recordValue != ip)
        {
            // Update
            var putData = new JObject
            {
                ["type"] = type,
                ["name"] = fullDomain,
                ["content"] = ip,
                ["ttl"] = 120,
                ["proxied"] = false
            };

            var putRequest = new HttpRequestMessage(HttpMethod.Put, $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{recordId}");
            AddAuthHeaders(putRequest, _secretId, _secretKey);
            putRequest.Content = new StringContent(putData.ToString(), Encoding.UTF8, "application/json");

            var putResponse = await HttpClientInstance.SendAsync(putRequest);
            if (!putResponse.IsSuccessStatusCode)
            {
                string err = await putResponse.Content.ReadAsStringAsync();
                throw new Exception($"Cloudflare 更新记录失败: {err}");
            }
        }
    }

    private void AddAuthHeaders(HttpRequestMessage request, string secretId, string secretKey)
    {
        if (string.IsNullOrEmpty(secretId))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        }
        else
        {
            request.Headers.Add("X-Auth-Email", secretId);
            request.Headers.Add("X-Auth-Key", secretKey);
        }
    }
}
