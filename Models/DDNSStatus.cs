namespace MSLX.Plugin.DDNS.Models;

public class DDNSStatus
{
    public DateTime? LastRunTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public string LastErrorMessage { get; set; } = "";
    public bool IsRunning { get; set; }
    public string CurrentIP4 { get; set; } = "";
    public string CurrentIP6 { get; set; } = "";
}
