using System.Reflection;

namespace OpenTelemetry.Instrumentation.Digma.Helpers;

public interface IActivityNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method);
}

public class MethodFullNameSchema : IActivityNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method)
    {
        return $"{classType.FullName}.{method.Name}";
    }
}

public class ClassAndMethodNameSchema : IActivityNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method)
    {
        return $"{classType.Name}.{method.Name}";
    }
}