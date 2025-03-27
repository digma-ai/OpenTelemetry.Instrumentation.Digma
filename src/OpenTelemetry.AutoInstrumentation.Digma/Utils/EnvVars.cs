using System;
using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public static class EnvVars
{
    public static string DIGMA_AUTOINST_RULES_FILE = Environment.GetEnvironmentVariable("DIGMA_AUTOINST_RULES_FILE");
    public static string OTEL_DOTNET_AUTO_NAMESPACES = Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_NAMESPACES");

    public static Dictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>
        {
            ["DIGMA_AUTOINST_RULES_FILE"] = DIGMA_AUTOINST_RULES_FILE,
            ["OTEL_DOTNET_AUTO_NAMESPACES"] = OTEL_DOTNET_AUTO_NAMESPACES,
        };
    }
}