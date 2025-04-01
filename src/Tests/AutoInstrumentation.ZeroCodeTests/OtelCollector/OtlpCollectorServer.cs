using OpenTelemetry.Proto.Collector.Trace.V1;
using Grpc.Core;

namespace AutoInstrumentation.ZeroCodeTests.OtelCollector;

public class OtlpCollectorServer : IAsyncDisposable
{
    private readonly Server _server;
    private readonly OtlpCollector _collectorImpl = new();

    private OtlpCollectorServer()
    {
        var serverPort = new ServerPort("localhost", 0, ServerCredentials.Insecure);
        _server = new Server
        {
            Services = { TraceService.BindService(_collectorImpl) },
            Ports = { serverPort }
        };
        _server.Start();
        
        // Get the dynamic port
        Port = _server.Ports.First().BoundPort;
    }
    
    public int Port { get; }

    public static OtlpCollectorServer Start()
    {
        return new OtlpCollectorServer();
    }

    public async ValueTask DisposeAsync()
    {
        await _server.ShutdownAsync();
    }
    
    private class OtlpCollector : TraceService.TraceServiceBase
    {
        public List<ExportTraceServiceRequest> ReceivedSpans { get; } = new();

        public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
        {
            ReceivedSpans.Add(request);
            Console.WriteLine("Received Trace");
            return Task.FromResult(new ExportTraceServiceResponse());
        }
    } 
}