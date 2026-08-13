using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MSLX.Plugin.DDNS.Core.Providers;

public class HuaweiCloudProvider : IDNSProvider
{
    private readonly string _accessKeyId;
    private readonly string _accessKeySecret;
    private static readonly HttpClient _httpClient = new HttpClient();

    public HuaweiCloudProvider(string accessKeyId, string accessKeySecret)
    {
        _accessKeyId = accessKeyId;
        _accessKeySecret = accessKeySecret;
    }

    public async Task UpdateRecordAsync(string domain, string subDomain, string ip, string type)
    {
        // 1. Get Zone ID
        string zoneId = await GetZoneIdAsync(domain);
        if (string.IsNullOrEmpty(zoneId))
        {
            throw new Exception($"华为云未找到域名 {domain} 的 Zone 记录");
        }

        // 2. Find existing record
        string fullDomain = subDomain == "@" ? domain : $"{subDomain}.{domain}";
        if (!fullDomain.EndsWith(".")) fullDomain += ".";

        var getUri = new Uri($"https://dns.myhuaweicloud.com/v2/zones/{zoneId}/recordsets?type={type}&name={fullDomain}");
        var getRequest = new HttpRequestMessage(HttpMethod.Get, getUri);
        await SignRequestAsync(getRequest);

        var getResponse = await _httpClient.SendAsync(getRequest);
        getResponse.EnsureSuccessStatusCode();

        string getRespContent = await getResponse.Content.ReadAsStringAsync();
        var getJobj = JObject.Parse(getRespContent);
        var recordsets = getJobj["recordsets"] as JArray;

        string recordId = null;
        string recordValue = null;

        if (recordsets != null && recordsets.Count > 0)
        {
            recordId = recordsets[0]["id"]?.ToString();
            var records = recordsets[0]["records"] as JArray;
            if (records != null && records.Count > 0)
            {
                recordValue = records[0]?.ToString();
            }
        }

        // 3. Update or Create
        if (string.IsNullOrEmpty(recordId))
        {
            // Create
            var postData = new JObject
            {
                ["name"] = fullDomain,
                ["type"] = type,
                ["ttl"] = 300,
                ["records"] = new JArray { ip }
            };

            var postUri = new Uri($"https://dns.myhuaweicloud.com/v2/zones/{zoneId}/recordsets");
            var postRequest = new HttpRequestMessage(HttpMethod.Post, postUri);
            postRequest.Content = new StringContent(postData.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");

            await SignRequestAsync(postRequest);

            var postResponse = await _httpClient.SendAsync(postRequest);
            if (!postResponse.IsSuccessStatusCode)
            {
                string err = await postResponse.Content.ReadAsStringAsync();
                throw new Exception($"华为云添加记录失败: {err}");
            }
        }
        else if (recordValue != ip)
        {
            // Update
            var putData = new JObject
            {
                ["name"] = fullDomain,
                ["type"] = type,
                ["ttl"] = 300,
                ["records"] = new JArray { ip }
            };

            var putUri = new Uri($"https://dns.myhuaweicloud.com/v2/zones/{zoneId}/recordsets/{recordId}");
            var putRequest = new HttpRequestMessage(HttpMethod.Put, putUri);
            putRequest.Content = new StringContent(putData.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");

            await SignRequestAsync(putRequest);

            var putResponse = await _httpClient.SendAsync(putRequest);
            if (!putResponse.IsSuccessStatusCode)
            {
                string err = await putResponse.Content.ReadAsStringAsync();
                throw new Exception($"华为云更新记录失败: {err}");
            }
        }
    }

    private async Task<string> GetZoneIdAsync(string rootDomain)
    {
        var uri = new Uri($"https://dns.myhuaweicloud.com/v2/zones?name={rootDomain}");
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        await SignRequestAsync(request);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"华为云获取 Zone 失败: {err}");
        }

        var respContent = await response.Content.ReadAsStringAsync();
        var jobj = JObject.Parse(respContent);
        var zones = jobj["zones"] as JArray;
        
        if (zones != null && zones.Count > 0)
        {
            return zones[0]["id"]?.ToString();
        }
        return null;
    }

    private async Task SignRequestAsync(HttpRequestMessage request)
    {
        var time = DateTime.UtcNow;
        string timestamp = time.ToString("yyyyMMddTHHmmssZ");
        request.Headers.Add("X-Sdk-Date", timestamp);
        request.Headers.Host = request.RequestUri.Host;

        string method = request.Method.Method;
        string canonicalUri = request.RequestUri.AbsolutePath;
        if (!canonicalUri.EndsWith("/")) canonicalUri += "/";

        string canonicalQuery = "";
        if (!string.IsNullOrEmpty(request.RequestUri.Query))
        {
            var query = request.RequestUri.Query.TrimStart('?');
            var pairs = query.Split('&');
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=');
                if (kv.Length == 2)
                {
                    dict[Uri.EscapeDataString(Uri.UnescapeDataString(kv[0]))] = Uri.EscapeDataString(Uri.UnescapeDataString(kv[1]));
                }
            }
            canonicalQuery = string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        var headers = new Dictionary<string, string>
        {
            { "host", request.RequestUri.Host },
            { "x-sdk-date", timestamp }
        };

        var sortedHeaders = headers.OrderBy(kv => kv.Key, StringComparer.Ordinal);
        string canonicalHeaders = string.Join("", sortedHeaders.Select(kv => $"{kv.Key}:{kv.Value}\n"));
        string signedHeaders = string.Join(";", sortedHeaders.Select(kv => kv.Key));

        string payload = "";
        if (request.Content != null)
        {
            payload = await request.Content.ReadAsStringAsync();
        }

        string payloadHash = ToHex(SHA256(payload));

        string canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        string stringToSign = $"SDK-HMAC-SHA256\n{timestamp}\n{ToHex(SHA256(canonicalRequest))}";

        string signature = ToHex(HMACSHA256(_accessKeySecret, stringToSign));
        string authHeader = $"SDK-HMAC-SHA256 Access={_accessKeyId}, SignedHeaders={signedHeaders}, Signature={signature}";

        request.Headers.TryAddWithoutValidation("Authorization", authHeader);
    }

    private byte[] SHA256(string text)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            return sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }

    private byte[] HMACSHA256(string key, string text)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }

    private string ToHex(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
