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

    private static string GetLogFilePath()
    {
        try
        {
            var dataPath = MSLXPluginEntry.Instance?.Config()?.GetDataPath();
            if (!string.IsNullOrEmpty(dataPath))
            {
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                return Path.Combine(dataPath, "ddns.log");
            }
        }
        catch
        {
            // fallback if DataPath fails
        }
        return Path.Combine(AppContext.BaseDirectory, "ddns.log");
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
            lock (_fileLock)
            {
                File.AppendAllLines(GetLogFilePath(), new[] { logLine });
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
