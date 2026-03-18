using System;
using System.IO;
using UnityEngine;

public static class RuntimeFileLogger
{
    private static readonly object Sync = new object();
    private static string _logPath;
    private static bool _initialized;
    private static bool _fileLoggingEnabled;

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

            _initialized = true;
            _fileLoggingEnabled = false;

            try
            {
                string logDir = ResolveLogDirectory();
                if (string.IsNullOrWhiteSpace(logDir))
                    return;

                Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, "safechem-runtime.log");
                File.WriteAllText(_logPath, "==== SafeChem Runtime Log ====\n");
                _fileLoggingEnabled = true;

                WriteLine("INFO", "RuntimeFileLogger", "Log initialized at " + _logPath);
            }
            catch (Exception exception)
            {
                _logPath = string.Empty;
                _fileLoggingEnabled = false;
                Debug.LogWarning("[RuntimeFileLogger] File logging disabled: " + exception.Message);
            }
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

            if (_fileLoggingEnabled && !string.IsNullOrWhiteSpace(_logPath))
            {
                try
                {
                    File.AppendAllText(_logPath, line);
                }
                catch (Exception exception)
                {
                    _fileLoggingEnabled = false;
                    Debug.LogWarning("[RuntimeFileLogger] File append failed, fallback to console only: " + exception.Message);
                }
            }

            string consoleLine = "[" + source + "] " + message;
            if (string.Equals(level, "ERROR", StringComparison.Ordinal))
                Debug.LogError(consoleLine);
            else if (string.Equals(level, "WARN", StringComparison.Ordinal))
                Debug.LogWarning(consoleLine);
            else
                Debug.Log(consoleLine);
        }
    }

    private static string ResolveLogDirectory()
    {
        try
        {
            if (Application.isEditor)
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                if (!string.IsNullOrWhiteSpace(projectRoot))
                    return Path.Combine(projectRoot, "Logs");
            }
        }
        catch
        {
        }

        string persistent = Application.persistentDataPath;
        if (string.IsNullOrWhiteSpace(persistent))
            return null;

        return Path.Combine(persistent, "Logs");
    }
}
