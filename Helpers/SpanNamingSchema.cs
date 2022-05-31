using System.Reflection;

namespace OpenTelemetry.Instrumentation.Digma.Helpers;

public interface ISpanNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method);
}

public class MethodFullNameSchema : ISpanNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method)
    {
        return $"{classType.FullName}.{method.Name}";
    }
}

public class ClassAndMethodNameSchema : ISpanNamingSchema
{
    public string GetSpanName(Type classType, MethodInfo method)
    {
        return $"{classType.Name}.{method.Name}";
    }
}