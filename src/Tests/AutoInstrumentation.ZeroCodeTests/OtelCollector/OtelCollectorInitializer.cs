using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Proto.Collector.Trace.V1;

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
        await _otlpCollectorServer.DisposeAsync();
        Console.WriteLine("Otel collector was stopped");
    }

    public static int Port => _otlpCollectorServer.Port;
    
    public static IReadOnlyList<ExportTraceServiceRequest> ReceivedSpans => _otlpCollectorServer.ReceivedSpans;
}