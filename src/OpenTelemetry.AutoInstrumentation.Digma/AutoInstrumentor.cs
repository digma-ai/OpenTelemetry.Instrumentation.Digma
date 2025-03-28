using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma;

public class AutoInstrumentor : IDisposable
{
    private readonly Harmony _harmony;
    private readonly SqlClientInstrumentation _sqlClientInstrumentation;
    private readonly VerticaInstrumentation _verticaInstrumentation;
    private readonly UserCodeInstrumentation _userCodeInstrumentation;
    private readonly HashSet<Assembly> _scannedAssemblies = new();
    
    public AutoInstrumentor(Configuration configuration = null)
    {
        _harmony = new Harmony("OpenTelemetry.AutoInstrumentation.Digma");
        _sqlClientInstrumentation = new SqlClientInstrumentation(_harmony);
        _verticaInstrumentation = new VerticaInstrumentation(_harmony);
        _userCodeInstrumentation = new UserCodeInstrumentation(_harmony, configuration);
    }
    
    public AutoInstrumentor Instrument()
    {
        Logger.LogInfo("Sync Initialization started");
        Logger.LogInfo("Env vars:\n"+string.Join("\n", EnvVars.GetAll().Select(x => $"{x.Key}={x.Value}")));
        
        AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
        {
            Logger.LogDebug($"Processing lazy-loaded {args.LoadedAssembly.FullName}");
            ProcessAssembly(args.LoadedAssembly);
        };
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Logger.LogDebug($"Processing pre-loaded assembly {assembly.FullName}");
            ProcessAssembly(assembly);
        }

        return this;
    }
    
    private void ProcessAssembly(Assembly assembly)
    {
        lock (_scannedAssemblies)
        {
            if(!_scannedAssemblies.Add(assembly))
                return;
        }
        
        var name = assembly.GetName().Name;
        if (name == "System.Data")
        {
            _sqlClientInstrumentation.Instrument(assembly);
            return;
        }

        if (name == "Vertica.Data")
        {
            _verticaInstrumentation.Instrument(assembly);
            return;
        }

        _userCodeInstrumentation.Instrument(assembly);
    }

    public void Dispose()
    {
        _harmony.UnpatchAll(_harmony.Id);
    }
}