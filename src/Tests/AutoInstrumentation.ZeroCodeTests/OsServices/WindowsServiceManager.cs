using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AutoInstrumentation.ZeroCodeTests.OsServices;

public class WindowsServiceManager
{
    public bool ServiceExists(string serviceName)
    {
        return GetService(serviceName) != null;
    }

    public bool IsServiceRunning(string serviceName)
    {
        return GetService(serviceName)?.Status == ServiceControllerStatus.Running;
    }

    public void CreateService(string serviceName, string exeFilePath)
    {
        RunSc("create", serviceName, "binpath=", exeFilePath);
        EditServiceEnvironmentVariables(serviceName, x =>
        {
            x["SERVICE_NAME"] = serviceName;
        });
    }
    
    public void InstrumentService(string serviceName, string otelCollectorUrl, string otelCollectorProtocol)
    {
        var command = @$"
            Import-Module '{Path.Combine(Directory.GetCurrentDirectory(), "OpenTelemetry.DotNet.Auto.psm1")}';
            Register-OpenTelemetryForWindowsService -WindowsServiceName '{serviceName}' -OTelServiceName '{serviceName}';
        ";
        RunPowershell("-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $"\"{command}\"");
        WaitForStatus(serviceName, ServiceControllerStatus.Running);

        Console.WriteLine("Set otel collector env vars");
        EditServiceEnvironmentVariables(serviceName, x =>
        {
            x["OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES"] = "*";
            x["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = otelCollectorUrl;
            x["OTEL_EXPORTER_OTLP_PROTOCOL"] = otelCollectorProtocol; // OTEL_EXPORTER_OTLP_TRACES_PROTOCOL doesn't work!
            x["OTEL_DOTNET_AUTO_PLUGINS"] = "OpenTelemetry.AutoInstrumentation.Digma.Plugin, OpenTelemetry.AutoInstrumentation.Digma";
            x["OTEL_LOG_LEVEL"] = "debug";
        });
        RunPowershell("Restart-Service", serviceName);
        WaitForStatus(serviceName, ServiceControllerStatus.Running);
    }
    
    public void DeleteService(string serviceName)
    {
        if(GetService(serviceName)?.Status == ServiceControllerStatus.Running)
            RunSc("stop", serviceName);
        WaitForStatus(serviceName, ServiceControllerStatus.Stopped);
        RunSc("delete", serviceName);
    }

    private ServiceController? GetService(string serviceName)
    {
        return ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == serviceName);
    }

    private void EditServiceEnvironmentVariables(string serviceName, Action<Dictionary<string, string>> edit)
    {
        var keyName = @$"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{serviceName}";
        var valueName = "Environment";
        var key = Registry.GetValue(keyName, valueName, Array.Empty<string>());

        var map = new Dictionary<string, string>();
        foreach (var item in (string[]) key)
        {
            var parts = item.Split('=', 2);
            map[parts[0]] = parts[1];
        }
        edit(map);
        var newValue = map.Select(x => $"{x.Key}={x.Value}").ToArray();
        
        Registry.SetValue(keyName, valueName, newValue);
    }
    
    private void WaitForStatus(string serviceName, ServiceControllerStatus status)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            if(GetService(serviceName)?.Status == status)
                return;
            Thread.Sleep(500);
        }

        throw new System.TimeoutException();
    }
    
    private void RunSc(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Console.WriteLine($"Running: {startInfo.FileName} {startInfo.Arguments}");
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode == 5)
            throw new Exception("sc.exe failed on insufficient permissions. Start Rider as Administrator and try again\n"+output);
        if (process.ExitCode != 0)
            throw new Exception("sc.exe failed:\n" + output);
    }   
    
    private void RunPowershell(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Console.WriteLine($"Running: {startInfo.FileName} {startInfo.Arguments}");
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var output = process.StandardOutput.ReadToEnd() + "\n" +
                     process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception("powershell failed:\n" + output);
    }
}