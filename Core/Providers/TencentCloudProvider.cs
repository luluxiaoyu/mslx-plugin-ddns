using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Dnspod.V20210323;
using TencentCloud.Dnspod.V20210323.Models;

namespace MSLX.Plugin.DDNS.Core.Providers;

public class TencentCloudProvider : IDNSProvider
{
    private readonly DnspodClient _client;

    public TencentCloudProvider(string secretId, string secretKey)
    {
        Credential cred = new Credential { SecretId = secretId, SecretKey = secretKey };
        ClientProfile clientProfile = new ClientProfile();
        HttpProfile httpProfile = new HttpProfile { Endpoint = "dnspod.tencentcloudapi.com" };
        clientProfile.HttpProfile = httpProfile;
        _client = new DnspodClient(cred, "", clientProfile);
    }

    public async Task UpdateRecordAsync(string domain, string subDomain, string ip, string type)
    {
        // 1. 获取记录列表
        var req = new DescribeRecordListRequest
        {
            Domain = domain,
            Subdomain = subDomain,
            RecordType = type
        };
        var resp = await _client.DescribeRecordList(req);

        if (resp.RecordList == null || resp.RecordList.Length == 0)
        {
            // 不存在则创建
            var createReq = new CreateRecordRequest
            {
                Domain = domain,
                SubDomain = subDomain,
                RecordType = type,
                RecordLine = "默认",
                Value = ip
            };
            await _client.CreateRecord(createReq);
            return;
        }

        var record = resp.RecordList[0];
        if (record.Value == ip) return;

        // 2. 更新记录
        var modReq = new ModifyRecordRequest
        {
            Domain = domain,
            SubDomain = subDomain,
            RecordType = type,
            RecordLine = "默认",
            Value = ip,
            RecordId = record.RecordId
        };
        await _client.ModifyRecord(modReq);
    }
}
