using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public static class MethodDiscovery
{
    public static MethodInfo[] GetMethodsToPatch(Assembly assembly, Configuration configuration)
    {
        var methodsToPatch = new List<MethodInfo>();
        
        foreach (var type in assembly.GetTypes().Where(CanInstrumentType))
        {
            // Match namespace+class
            var typeIncludeRules = GetMatchingRules(type, configuration.Include).ToArray();
            var typeExcludeRules = GetMatchingRules(type, configuration.Exclude).ToArray();
            
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
                
                methodsToPatch.Add(method);
            }
        }

        return methodsToPatch.ToArray();
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
                        (r.AccessModifier == MethodAccessModifier.Private && !method.IsPublic) ||
                        (r.AccessModifier == MethodAccessModifier.Public && method.IsPublic))
            .Where(r => r.SyncModifier == null ||
                        (r.SyncModifier == MethodSyncModifier.Async && IsAsyncMethod(method)) ||
                        (r.SyncModifier == MethodSyncModifier.Sync && !IsAsyncMethod(method)))
            .ToArray();
    }
    
    
    private static bool IsMatched(string pattern, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || 
            string.IsNullOrWhiteSpace(pattern))
            return true;
        
        var regexPattern = InstrumentationRule.IsRegex(pattern)
            ? pattern.Trim('/')
            : "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";

        return Regex.IsMatch(text, regexPattern);
    }

    private static bool CanInstrumentType(Type type)
    {
        return !typeof(Delegate).IsAssignableFrom(type) &&
               !type.IsGenericType &&
               !type.IsAbstract;
    }
    
    private static bool CanInstrumentMethod(Type type, MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType == type &&
               !methodInfo.IsAbstract &&
               !methodInfo.IsSpecialName && // property accessors and operator overloading methods
               !methodInfo.IsGenericMethod &&
               methodInfo.Name != "GetHashCode" && 
               methodInfo.Name != "Equals" && 
               methodInfo.Name != "ToString" && 
               methodInfo.Name != "Deconstruct" &&
               methodInfo.Name != "MoveNext" &&
               methodInfo.Name != "SetStateMachine" &&
               methodInfo.Name != "PrintMembers" &&
               methodInfo.Name != "Dispose" &&
               methodInfo.Name != "GetEnumerator" &&
               methodInfo.Name != "<Clone>$";
    }

    private static bool IsAsyncMethod(MethodInfo methodInfo)
    {
        return typeof(Task).IsAssignableFrom(methodInfo.ReturnType) ||
               typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType) ||
               (methodInfo.ReturnType.IsGenericType && 
                typeof(ValueTask<>).IsAssignableFrom(methodInfo.ReturnType.GetGenericTypeDefinition()));
    }
}