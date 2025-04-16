using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public class Configuration
{
    public InstrumentationRule[] Include { get; set; } = Array.Empty<InstrumentationRule>();
    public InstrumentationRule[] Exclude { get; set; } = Array.Empty<InstrumentationRule>();
}

public class InstrumentationRule
{
    public string Namespaces { get; set; }
    public string Classes { get; set; }
    public string Methods { get; set; }
    public bool NestedOnly { get; set; }
    
    public MethodSyncModifier? SyncModifier { get; set; }
    
    public MethodAccessModifier? AccessModifier { get; set; }

    public static bool IsRegex(string matcher)
    {
        return matcher.StartsWith("/") && matcher.EndsWith("/");
    }
}

public enum MethodSyncModifier
{
    Async,
    Sync
}

public enum MethodAccessModifier
{
    Public,
    Private
}

