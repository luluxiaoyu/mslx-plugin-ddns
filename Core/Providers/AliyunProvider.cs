using System.Threading.Tasks;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using Tea;

namespace MSLX.Plugin.DDNS.Core.Providers;

public class AliyunProvider : IDNSProvider
{
    private readonly Client _client;

    public AliyunProvider(string secretId, string secretKey)
    {
        AlibabaCloud.OpenApiClient.Models.Config config = new AlibabaCloud.OpenApiClient.Models.Config
        {
            AccessKeyId = secretId,
            AccessKeySecret = secretKey,
            Endpoint = "alidns.cn-hangzhou.aliyuncs.com"
        };
        _client = new Client(config);
    }

    public async Task UpdateRecordAsync(string domain, string subDomain, string ip, string type)
    {
        // 1. 获取记录列表
        DescribeDomainRecordsRequest describeReq = new DescribeDomainRecordsRequest
        {
            DomainName = domain,
            RRKeyWord = subDomain,
            Type = type
        };
        
        var resp = await _client.DescribeDomainRecordsAsync(describeReq);
        
        var records = resp.Body.DomainRecords.Record;
        string recordId = null;
        string recordValue = null;

        foreach (var r in records)
        {
            if (r.RR == subDomain && r.Type == type)
            {
                recordId = r.RecordId;
                recordValue = r.Value;
                break;
            }
        }

        if (string.IsNullOrEmpty(recordId))
        {
            // 创建
            AddDomainRecordRequest addReq = new AddDomainRecordRequest
            {
                DomainName = domain,
                RR = subDomain,
                Type = type,
                Value = ip
            };
            await _client.AddDomainRecordAsync(addReq);
        }
        else if (recordValue != ip)
        {
            // 更新
            UpdateDomainRecordRequest updateReq = new UpdateDomainRecordRequest
            {
                RecordId = recordId,
                RR = subDomain,
                Type = type,
                Value = ip
            };
            await _client.UpdateDomainRecordAsync(updateReq);
        }
    }
}
