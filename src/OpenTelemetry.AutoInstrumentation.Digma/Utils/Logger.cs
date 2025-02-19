using System;
using System.IO;
using System.Threading;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public static class Logger
{
    private static readonly string FilePath;
    private static int _recordedError;

    private enum Level
    {
        Debug,
        Info,
        Error
    }
    
    static Logger()
    {
        var logDir = Environment.GetEnvironmentVariable("SQL_CLIENT_PATCH_LOG_DIR");
        if (string.IsNullOrWhiteSpace(logDir))
            logDir = @"c:\";
        FilePath = Path.Combine(logDir, "OpenTelemetry.AutoInstrumentation.Digma.Log.txt");
    }

    public static void LogDebug(string message)
    {
        Log(Level.Debug, message);
    }
    
    public static void LogInfo(string message)
    {
        Log(Level.Info, message);
    }
    
    public static void LogError(string message)
    {
        Log(Level.Error, message);
    }    
    
    public static void LogError(string message, Exception ex)
    {
        Log(Level.Error, $"{message}\n{ex}");
    }
    
    private static void Log(Level level, string message)
    {
        try
        {
            var contents = $"[{DateTime.Now:o}] [{level.ToString().ToUpper()}] {message}\n";
            Console.Write(contents);
            File.AppendAllText(FilePath, contents);
        }
        catch (Exception e)
        {
            if(Interlocked.Exchange(ref _recordedError, 1) == 0)
                Console.WriteLine("Failed to write log: " + e);
        }
    }
}