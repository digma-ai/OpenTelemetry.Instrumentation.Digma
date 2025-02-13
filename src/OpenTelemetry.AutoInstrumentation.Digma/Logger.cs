using System;
using System.IO;

namespace OpenTelemetry.AutoInstrumentation.Digma;

public static class Logger
{
    private static readonly string FilePath;

    static Logger()
    {
        var logDir = Environment.GetEnvironmentVariable("SQL_CLIENT_PATCH_LOG_DIR");
        if (string.IsNullOrWhiteSpace(logDir))
            logDir = @"c:\";
        FilePath = Path.Combine(logDir, "SqlClientPatchLog.txt");
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(FilePath, $"[{DateTime.Now:u}] SqlClientPatch - {message}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to write log: " + e);
        }
    }
}