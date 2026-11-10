using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSLX.SDK;

namespace MSLX.Plugin.DDNS.Core;

public static class DDNSLogger
{
    private static readonly ConcurrentQueue<string> _logs = new();
    private static readonly object _fileLock = new();
    private const int MaxLogCount = 100;

    private static string? GetLogFilePath()
    {
        try
        {
            var dataPath = MSLXPluginEntry.Instance?.Config()?.GetDataPath();
            if (!string.IsNullOrEmpty(dataPath))
            {
                var fullBasePath = Path.GetFullPath(dataPath);
                if (!Directory.Exists(fullBasePath))
                {
                    Directory.CreateDirectory(fullBasePath);
                }
                var targetPath = Path.GetFullPath(Path.Combine(fullBasePath, "ddns.log"));
                if (targetPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    return targetPath;
                }
            }
        }
        catch
        {
            // fallback if DataPath fails
        }
        return null;
    }

    public static void LogInfo(string message, bool toSystemLog = false)
    {
        string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";
        AddLog(formatted);

        if (toSystemLog)
        {
            SDK.MSLX.Logger.Info($"[DDNS] {message}");
        }
    }

    public static void LogError(string message)
    {
        string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
        AddLog(formatted);

        SDK.MSLX.Logger.Error($"[DDNS] {message}");
    }

    private static void AddLog(string logLine)
    {
        _logs.Enqueue(logLine);
        while (_logs.Count > MaxLogCount)
        {
            _logs.TryDequeue(out _);
        }

        // 异步写入或简单写文件
        try
        {
            var logPath = GetLogFilePath();
            if (logPath != null)
            {
                lock (_fileLock)
                {
                    File.AppendAllLines(logPath, new[] { logLine });
                }
            }
        }
        catch
        {
            // 忽略写文件并发异常
        }
    }

    public static List<string> GetLogs(int limit = 50)
    {
        return _logs.TakeLast(limit).ToList();
    }
}
