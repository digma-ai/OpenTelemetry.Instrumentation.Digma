using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.Digma;

public static class SpanUtils
{
    public static void AddCommonTags(MethodInfo methodInfo, Activity? activity)
    {
        activity?.AddTag("code.namespace", methodInfo.DeclaringType?.ToString());
        activity?.AddTag("code.function", methodInfo.Name);
        activity?.AddTag("code.function.parameter.types", BuildParameterTypes(methodInfo));
    }

    static string BuildParameterTypes(MethodInfo methodInfo)
    {
        ParameterInfo[] paramInfos = methodInfo.GetParameters();
        if (paramInfos.Length <= 0)
        {
            return "";
        }

        return string.Join('|', paramInfos.Select(pi => pi.ParameterType.FullName));
    }
}