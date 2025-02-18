using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma;

public class Plugin
{
    private static readonly ActivitySource ActivitySource = new("OpenTelemetry.AutoInstrumentation.Digma");
    private static readonly MethodInfo PrefixMethodInfo = typeof(Plugin).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
    // private readonly MethodInfo _postfixMethodInfo = typeof(Plugin).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo FinalizerMethodInfo = typeof(Plugin).GetMethod(nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo SqlTranspilerMethodInfo = typeof(Plugin).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic);

    private readonly Harmony _harmony;
    private readonly string[] _namespaces;
    private readonly bool _includePrivateMethods;
    
    public Plugin()
    {
        _harmony = new Harmony("OpenTelemetry.AutoInstrumentation.Digma");
        var namespacesStr = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_NAMESPACES");
        _namespaces = namespacesStr?.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray()
            ?? Array.Empty<string>();
        _includePrivateMethods = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PRIVATE_METHODS")
            ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true;
    }
    
    public void Initializing()
    {
        Logger.LogInfo("Initialization started");
        Logger.LogInfo($"Requested to auto-instrument {_namespaces.Length} namespaces:\n"+
                       string.Join("\n", _namespaces));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Logger.LogInfo($"Processing pre-loaded assembly {assembly.FullName}");
            ProcessAssembly(assembly);
        }
        
        AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
        {
            Logger.LogInfo($"Processing lazy-loaded {args.LoadedAssembly.FullName}");
            ProcessAssembly(args.LoadedAssembly);
        };
    }

    private void ProcessAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == "System.Data")
        {
            PatchSqlClient(assembly);
            return;
        }

        if (ShouldInstrumentAssembly(name))
        {
            var relevantTypes = assembly.GetTypes().Where(ShouldInstrumentType).ToArray();
            var methods = relevantTypes
                .SelectMany(t => t
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => ShouldInstrumentMethod(t,m)))
                .ToArray();
            foreach (var method in methods)
            {
                PatchMethod(method);
            }
        }
    }

    private bool ShouldInstrumentAssembly(string assemblyName)
    {
        return _namespaces.Any(ns => ns.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldInstrumentType(Type type)
    {
        return _namespaces.Any(ns => type.FullName?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) == true);
    }

    private bool ShouldInstrumentMethod(Type type, MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType == type &&
               !methodInfo.IsAbstract &&
               (methodInfo.IsPublic || _includePrivateMethods);
    }

    private bool DoesAlreadyStartActivity(MethodInfo methodInfo)
    {
        var instructions = PatchProcessor.GetOriginalInstructions(methodInfo);
        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo call &&
                call.DeclaringType == typeof(ActivitySource) &&
                call.Name == nameof(System.Diagnostics.ActivitySource.StartActivity))
            {
                return true;
            }
        }

        return false;
    }
    
    private void PatchMethod(MethodInfo originalMethodInfo)
    {
        var methodFullName = $"Patching {originalMethodInfo.GetType().FullName}.{originalMethodInfo.Name}";
        try
        {
            var prefix = DoesAlreadyStartActivity(originalMethodInfo)
                ? null
                : new HarmonyMethod(PrefixMethodInfo);

            var finalizer = new HarmonyMethod(FinalizerMethodInfo);

            _harmony.Patch(originalMethodInfo, prefix: prefix, finalizer: finalizer);
            Logger.LogInfo($"Patched method {methodFullName}"+(prefix==null?" (partially)":""));
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to patch {methodFullName}", e);
        }

    }
    
    private void PatchSqlClient(Assembly systemDataAssembly)
    {
        try
        {
            var sqlCommandType = systemDataAssembly.GetType("System.Data.SqlClient.SqlCommand", throwOnError: false);
            if (sqlCommandType == null)
            {
                Logger.LogError("System.Data.SqlClient.SqlCommand not found.");
                return;
            }

            var targetMethodInfo =
                sqlCommandType.GetMethod("WriteBeginExecuteEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (targetMethodInfo == null)
            {
                Logger.LogError("WriteBeginExecuteEvent not found.");
                return;
            }

            _harmony.Patch(targetMethodInfo, transpiler: new HarmonyMethod(SqlTranspilerMethodInfo));
            Logger.LogInfo("Patched SqlClient");
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to patch System.Data.SqlClient.SqlCommand.WriteBeginExecuteEvent", e);
        }
    }

    private static void Prefix(MethodBase __originalMethod, out Activity __state)
    {
        var activity = ActivitySource.StartActivity(__originalMethod.Name);
        activity?.SetTag(DigmaSemanticConventions.ExtendedObservabilityPackage, __originalMethod.DeclaringType?.Assembly.GetName().Name);
        activity?.SetTag(DigmaSemanticConventions.CodeNamespace, __originalMethod.DeclaringType?.FullName);
        activity?.SetTag(DigmaSemanticConventions.CodeFunction, __originalMethod.Name);
        __state = activity;
    }
    
    // private static void Postfix(MethodBase __originalMethod, Activity __state)
    // {
    //     // __state?.Dispose();
    //     Logger.Log($"Postfix - {__originalMethod.Name}");
    // }
    
    private static void Finalizer(MethodBase __originalMethod, Activity __state, Exception __exception)
    {
        var activity = __state;
        
        if (__exception != null)
        {
            // Record exception details even if the activity wasn't opened by us
            // BUT modify the status only it is ours
            Activity.Current?.RecordException(__exception);
            activity?.SetStatus(ActivityStatusCode.Error);
        }
        
        activity?.Dispose();
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = new List<CodeInstruction>(instructions);

        Logger.LogDebug($"Before ({instructionList.Count}):\n"+ 
                        string.Join("\n",instructionList.Select(x => x.ToString()).ToArray()));

        for (var i = 0; i < instructionList.Count-1; i++)
        {
            if (instructionList[i].opcode == OpCodes.Ldarg_0 && // "this"
                instructionList[i+1].operand.ToString().Contains("System.Data.CommandType get_CommandType()"))
            {
                instructionList.RemoveRange(i, 6); 
                break;
            }
        }
        
        Logger.LogDebug($"After ({instructionList.Count}):\n"+ 
                        string.Join("\n",instructionList.Select(x => x.ToString()).ToArray()));

        return instructionList;
    }
}