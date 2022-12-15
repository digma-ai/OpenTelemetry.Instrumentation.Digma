using System.Diagnostics;
using System.Reflection;

namespace OpenTelemetry.Instrumentation.Digma;

public static class SpanUtils
{
    public static void AddCommonTags(Type classType, MethodInfo methodInfo, Activity? activity)
    {
        activity?.AddTag("code.namespace", classType.FullName);
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

        return string.Join('|', paramInfos.Select(pi => pi.ParameterType.Name));
    }
}