using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;


namespace OpenTelemetry.AutoInstrumentation.Digma;

public class Plugin
{
    private readonly Harmony _harmony;
    private readonly SqlClientInstrumentation _sqlClientInstrumentation;
    private readonly VerticaInstrumentation _verticaInstrumentation;
    private readonly UserCodeInstrumentation _userCodeInstrumentation;
    private readonly HashSet<Assembly> _scannedAssemblies = new();
    
    public Plugin()
    {
        _harmony = new Harmony("OpenTelemetry.AutoInstrumentation.Digma");
        _sqlClientInstrumentation = new SqlClientInstrumentation(_harmony);
        _verticaInstrumentation = new VerticaInstrumentation(_harmony);
        _userCodeInstrumentation = new UserCodeInstrumentation(_harmony);
    }
    
    public void Initializing()
    {
        Logger.LogInfo("Initialization started");
        
       new Thread(o =>
       {
           Thread.Sleep(TimeSpan.FromSeconds(2));
           SyncInitializing();
       }).Start();
    }

    public void SyncInitializing()
    {
        Logger.LogInfo("Sync Initialization started");
        
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
}