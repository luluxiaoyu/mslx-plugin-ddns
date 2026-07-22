using Microsoft.AspNetCore.Mvc;
using MSLX.SDK;
using MSLX.Plugin.DDNS.Core;
using MSLX.Plugin.DDNS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MSLX.Plugin.DDNS.Controllers;

[Route("api/plugins/mslx-plugin-ddns/ddns")]
[ApiController]
public class DDNSController : ControllerBase
{
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var config = ConfigHelper.Load();

        // 隐藏 Key
        if (!string.IsNullOrEmpty(config.SecretKey))
        {
            config.SecretKey = "******";
        }
        
        return Ok(config);
    }

    [HttpPost("save-config")]
    public IActionResult SaveConfig([FromBody] DDNSConfig config)
    {
        var oldConfig = ConfigHelper.Load();
        
        if (string.IsNullOrEmpty(config.SecretKey) || config.SecretKey == "******")
        {
            config.SecretKey = oldConfig.SecretKey;
        }

        ConfigHelper.Save(config);

        DDNSLogger.LogInfo("插件配置已保存", toSystemLog: true);
        return Ok(new { msg = "success" });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(DDNSService.CurrentStatus);
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int limit = 50)
    {
        var logs = DDNSLogger.GetLogs(limit);
        return Ok(logs);
    }

    [HttpPost("request-now")]
    public IActionResult RequestNow()
    {
        if (DDNSService.CurrentStatus.IsRunning)
        {
            return BadRequest(new { message = "任务正在运行中，请勿重复触发" });
        }
        
        DDNSLogger.LogInfo("用户手动触发 IP 同步检查", toSystemLog: true);
        Task.Run(async () =>
        {
            await DDNSService.RunSyncAsync();
        });
        
        return Ok(new { msg = "ok" });
    }

    [HttpGet("nics")]
    public IActionResult GetNics()
    {
        var nics = IPHelper.GetNicNames();
        return Ok(nics);
    }
}
