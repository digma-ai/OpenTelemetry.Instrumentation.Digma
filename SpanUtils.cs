﻿using System.Diagnostics;
using System.Reflection;

namespace OpenTelemetry.Instrumentation.Digma;

public class SpanUtils
{
    public static void AddCommonTags(MethodInfo methodInfo, Activity? activity)
    {
        activity.AddTag("code.namespace", methodInfo.DeclaringType?.ToString());
        activity.AddTag("code.function", methodInfo.Name);
        activity.AddTag("code.function.parameter.types", BuildParameterTypes(methodInfo));
    }

    static string BuildParameterTypes(MethodInfo methodInfo)
    {
        var paramInfos = methodInfo.GetParameters();
        if (paramInfos == null || paramInfos.Length <= 0)
        {
            return "";
        }

        return string.Join('|', paramInfos.Select(pi => pi.ParameterType.FullName));
    }
}