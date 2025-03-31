using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;

public class UserCodeInstrumentation
{
    private static readonly ActivitySourceProvider ActivitySourceProvider = new();
    private static readonly MethodInfo PrefixMethodInfo = typeof(UserCodeInstrumentation).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo FinalizerMethodInfo = typeof(UserCodeInstrumentation).GetMethod(nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic);

    private readonly Harmony _harmony;
    private readonly Configuration _configuration;

    public UserCodeInstrumentation(Harmony harmony, Configuration configuration = null)
    {
        _harmony = harmony;
        _configuration = configuration ?? ConfigurationProvider.GetConfiguration();

        Logger.LogInfo("Configuration:\n" + _configuration.ToJson());
    }
    
    public void Instrument(Assembly assembly)
    {
        MethodInfo[] methods;
        try
        {
            methods = MethodDiscovery.GetMethodsToPatch(assembly, _configuration);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to discover methods to instrument in '{assembly.FullName}'", e);
            return;
        }

        foreach (var method in methods)
        {
            PatchMethod(method);
        }
    }
    
    private void PatchMethod(MethodInfo originalMethodInfo)
    {
        var methodFullName = $"{originalMethodInfo.DeclaringType?.FullName}.{originalMethodInfo.Name}";
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

    private static bool DoesAlreadyStartActivity(MethodInfo methodInfo)
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

    private static void Prefix(MethodBase __originalMethod, out Activity __state)
    {
        var activitySource = ActivitySourceProvider.GetOrCreate(__originalMethod.DeclaringType!);
        var activity = activitySource.StartActivity(__originalMethod.Name);
        Logger.LogDebug($"Opened Activity: {activity?.Source.Name}.{activity?.OperationName}");
        activity?.SetTag(DigmaSemanticConventions.ExtendedObservabilityPackage, __originalMethod.DeclaringType?.Assembly.GetName().Name);
        activity?.SetCodeTags(__originalMethod);
        __state = activity;
    }

    private static void Finalizer(MethodBase __originalMethod, Activity __state, Exception __exception)
    {
        var activity = __state;
        if (activity == null) 
            return;
        
        if (__exception != null)
        {
            activity.RecordException(__exception);
            activity.SetStatus(ActivityStatusCode.Error);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }            
        activity.Dispose();
        Logger.LogDebug($"Closed Activity: {activity.Source.Name}.{activity.OperationName}");
    }
}