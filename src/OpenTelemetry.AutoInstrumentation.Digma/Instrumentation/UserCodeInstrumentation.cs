using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;

public class UserCodeInstrumentation
{
    private static readonly ActivitySourceProvider ActivitySourceProvider = new();
    private static readonly MethodInfo PrefixMethodInfo = typeof(UserCodeInstrumentation).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
    public static readonly MethodInfo FinalizerMethodInfo = typeof(UserCodeInstrumentation).GetMethod(nameof(Finalizer), BindingFlags.Static | BindingFlags.NonPublic);

    private readonly Harmony _harmony;
    private readonly string[] _namespaces;
    private readonly bool _includePrivateMethods;

    public UserCodeInstrumentation(Harmony harmony)
    {
        _harmony = harmony;
        var namespacesStr = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_NAMESPACES");
        _namespaces = namespacesStr?.Split(',')
                          .Select(x => x.Trim())
                          .Where(x => !string.IsNullOrWhiteSpace(x))
                          .ToArray()
                      ?? Array.Empty<string>();
        _includePrivateMethods = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PRIVATE_METHODS")
            ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true;
        
        Logger.LogInfo($"Requested to auto-instrument {_namespaces.Length} namespaces:\n"+
                       string.Join("\n", _namespaces));
    }
    
    public void Instrument(Assembly assembly)
    {
        var name = assembly.GetName().Name;
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
    
    private bool ShouldInstrumentAssembly(string assemblyName)
    {
        return _namespaces.Any(ns => ns.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase) ||
                                     assemblyName.StartsWith(ns, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldInstrumentType(Type type)
    {
        return !typeof(Delegate).IsAssignableFrom(type) &&
               !type.IsGenericType &&
               _namespaces.Any(ns => type.FullName?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) == true);
    }

    private bool ShouldInstrumentMethod(Type type, MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType == type &&
               !methodInfo.IsAbstract &&
               !methodInfo.IsSpecialName && // property accessors and operator overloading methods
               !methodInfo.IsGenericMethod &&
               methodInfo.Name != "GetHashCode" && 
               methodInfo.Name != "Equals" && 
               methodInfo.Name != "ToString" && 
               methodInfo.Name != "Deconstruct" &&
               methodInfo.Name != "<Clone>$" &&
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
        activity.Dispose();
        Logger.LogDebug($"Closed Activity: {activity.Source.Name}.{activity.OperationName}");
    }
}