using System;
using System.IO;
using UnityEngine;

public static class RuntimeFileLogger
{
    private static readonly object Sync = new object();
    private static string _logPath;
    private static bool _initialized;

    public static string LogPath
    {
        get
        {
            EnsureInitialized();
            return _logPath;
        }
    }

    public static void Log(string source, string message)
    {
        EnsureInitialized();
        WriteLine("INFO", source, message);
    }

    public static void Warn(string source, string message)
    {
        EnsureInitialized();
        WriteLine("WARN", source, message);
    }

    public static void Error(string source, string message)
    {
        EnsureInitialized();
        WriteLine("ERROR", source, message);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Sync)
        {
            if (_initialized)
                return;

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string logDir = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, "safechem-runtime.log");
            File.WriteAllText(_logPath, "==== SafeChem Runtime Log ====\n");
            _initialized = true;

            WriteLine("INFO", "RuntimeFileLogger", "Log initialized at " + _logPath);
        }
    }

    private static void WriteLine(string level, string source, string message)
    {
        lock (Sync)
        {
            string line = string.Format(
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] {3}{4}",
                DateTime.Now,
                level,
                source,
                message,
                Environment.NewLine);
            File.AppendAllText(_logPath, line);
        }
    }
}
