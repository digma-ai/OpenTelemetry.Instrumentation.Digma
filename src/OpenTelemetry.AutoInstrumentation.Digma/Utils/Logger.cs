using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public static class Logger
{
    private static readonly string FilePath;
    private static readonly Level MinLevel;
    private static int _recordedError;

    private enum Level
    {
        None ,
        Debug,
        Info,
        Warn,
        Error
    }
    
    static Logger()
    {
        var levelStr = Environment.GetEnvironmentVariable("OTEL_LOG_LEVEL");
        if (string.IsNullOrWhiteSpace(levelStr) || !Enum.TryParse(levelStr, true, out MinLevel))
        {
            MinLevel = Level.Info;
        }
        
        var logDir = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY");
        if (string.IsNullOrWhiteSpace(logDir))
        {
            logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
        }
        var logFileName = GetLogFileName("digma");           
        FilePath = Path.Combine(logDir, logFileName);
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
        if (level < MinLevel)
            return;
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
    

    ////////////////////////////////////////////////////////////////////
    //
    // https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/src/OpenTelemetry.AutoInstrumentation/Logging/OtelLogging.cs
    //
    private static string GetLogFileName(string suffix)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var appDomainName = GetEncodedAppDomainName();

            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{process.Id}-{appDomainName}-.log"
                : $"otel-dotnet-auto-{process.Id}-{appDomainName}-{suffix}-.log";
        }
        catch
        {
            // We can't get the process info
            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{Guid.NewGuid()}-.log"
                : $"otel-dotnet-auto-{Guid.NewGuid()}-{suffix}-.log";
        }
    }
    private static string GetEncodedAppDomainName()
    {
        var name = AppDomain.CurrentDomain.FriendlyName;
        return name
            .Replace(Path.DirectorySeparatorChar, '-')
            .Replace(Path.AltDirectorySeparatorChar, '-')
            .Trim('-');
    }
    //
    ////////////////////////////////////////////////////////////////////
}