namespace OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class TraceActivityAttribute : Attribute
{
    public string? Name { get; }
    public bool RecordExceptions { get; }

    public TraceActivityAttribute(string? name = null, bool recordExceptions = true)
    {
        Name = name;
        RecordExceptions = recordExceptions;
    }
}