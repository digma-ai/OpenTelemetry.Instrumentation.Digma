using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using OpenTelemetry.Resources;
using Resource = OpenTelemetry.Proto.Resource.V1.Resource;

namespace AutoInstrumentation.ZeroCodeTests.OtelCollector;

[TestClass]
public class OtelCollectorInitializer
{
    private static OtlpCollectorServer _otlpCollectorServer;

    [AssemblyInitialize]
    public static async Task ApplicationInit(TestContext ctx)
    {
        Console.WriteLine("Otel collector is starting");
        _otlpCollectorServer = OtlpCollectorServer.Start();
        Console.WriteLine($"Otel collector is running on port {Port}");
    }

    [AssemblyCleanup]
    public static async Task ApplicationDown()
    {
        Console.WriteLine("Otel collector is stopping");
        _otlpCollectorServer.Dispose();
        Console.WriteLine("Otel collector was stopped");
    }

    public static int Port => _otlpCollectorServer.Port;
    
    public static (Resource Resource, InstrumentationScope Scope, Span Span)[] GetSpans(string serviceName)
    {
        return _otlpCollectorServer.ReceivedSpans
            .Where(x => x.Resource.Attributes.Any(a => a.Key == "service.name" &&
                                                       a.Value.StringValue == serviceName))
            .SelectMany(resource => resource.ScopeSpans
                .SelectMany(scope => scope.Spans
                    .Select(span => (resource.Resource, scope.Scope, Span: span))))
            .ToArray();
    }
}