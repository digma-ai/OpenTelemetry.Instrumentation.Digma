using System.Linq;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

static class DigmaSemanticConventions
{
    public const string CodeFunction = "code.function";
    public const string CodeNamespace = "code.namespace";
    public const string CodeFunctionParameterTypes = "code.function.parameter.types";
    public const string ExtendedObservabilityPackage = "digma.instrumentation.extended.package";
    public const string NestedOnly = "digma.nestedOnly";
    
    public static string BuildMethodParameterTypes(MethodInfo methodInfo)
    {
        var paramInfos = methodInfo.GetParameters();
        return paramInfos.Any()
            ? string.Join("|", paramInfos.Select(pi => pi.ParameterType.Name))
            : "";
    }
}