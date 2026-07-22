using System.Threading.Tasks;

namespace MSLX.Plugin.DDNS.Core.Providers;

public interface IDNSProvider
{
    Task UpdateRecordAsync(string domain, string subDomain, string ip, string type);
}
