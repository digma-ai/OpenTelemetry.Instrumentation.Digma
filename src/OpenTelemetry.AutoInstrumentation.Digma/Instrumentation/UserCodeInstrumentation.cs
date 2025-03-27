using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

    public UserCodeInstrumentation(Harmony harmony)
    {
        _harmony = harmony;
        _configuration = ConfigurationProvider.GetConfiguration();

        Logger.LogInfo("Configuration:\n" + _configuration.ToJson());
    }
    
    public void Instrument(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            // Match namespace+class
            var typeIncludeRules = GetMatchingRules(type, _configuration.Include).ToArray();
            var typeExcludeRules = GetMatchingRules(type, _configuration.Exclude).ToArray();
            
            if(!typeIncludeRules.Any())
                continue;
            
            // Match methods
            var methods = type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => CanInstrumentMethod(type, m));
            
            foreach (var method in methods)
            {
                var methodIncludeRules = GetMatchingRules(method, typeIncludeRules);
                var methodExcludeRules = GetMatchingRules(method, typeExcludeRules);
                
                if(!methodIncludeRules.Any() || methodExcludeRules.Any())
                    continue;
                
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

    private static InstrumentationRule[] GetMatchingRules(Type type, InstrumentationRule[] rules)
    {
        return rules.Where(r => IsMatched(r.Namespaces, type.Namespace) && IsMatched(r.Classes, type.Name)).ToArray();
    }
    
    private static InstrumentationRule[] GetMatchingRules(MethodInfo method, InstrumentationRule[] rules)
    {
        return rules
            .Where(r => IsMatched(r.Methods, method.Name))
            .Where(r => r.AccessModifier == null ||
                        (r.AccessModifier == MethodAccessModifier.Private && method.IsPrivate) ||
                        (r.AccessModifier == MethodAccessModifier.Public && method.IsPublic))
            .Where(r => r.SyncModifier == null ||
                        (r.SyncModifier == MethodSyncModifier.Async && IsAsyncMethod(method)) ||
                        (r.SyncModifier == MethodSyncModifier.Sync && !IsAsyncMethod(method)))
            .ToArray();
    }
    
    private static bool IsMatched(string pattern, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;
        
        var regexPattern = InstrumentationRule.IsRegex(pattern)
            ? pattern
            : "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";

        return Regex.IsMatch(text, regexPattern);
    }
    
    private bool CanInstrumentMethod(Type type, MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType == type &&
               !methodInfo.IsAbstract &&
               !methodInfo.IsSpecialName && // property accessors and operator overloading methods
               !methodInfo.IsGenericMethod &&
               methodInfo.Name != "GetHashCode" && 
               methodInfo.Name != "Equals" && 
               methodInfo.Name != "ToString" && 
               methodInfo.Name != "Deconstruct" &&
               methodInfo.Name != "<Clone>$";
    }

    private static bool IsAsyncMethod(MethodInfo methodInfo)
    {
        return typeof(Task).IsAssignableFrom(methodInfo.ReturnType) ||
               typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType) ||
               (methodInfo.ReturnType.IsGenericType && 
                typeof(ValueTask<>).IsAssignableFrom(methodInfo.ReturnType.GetGenericTypeDefinition()));
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
        activity.Dispose();
        Logger.LogDebug($"Closed Activity: {activity.Source.Name}.{activity.OperationName}");
    }
}