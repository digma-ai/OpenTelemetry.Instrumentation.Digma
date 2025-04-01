using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Grpc.Core;
using OpenTelemetry.Proto.Trace.V1;

namespace AutoInstrumentation.ZeroCodeTests.OtelCollector;

public class OtlpCollectorServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly ConcurrentBag<ResourceSpans> _receivedSpans = new();

    public IReadOnlyList<ResourceSpans> ReceivedSpans => _receivedSpans.ToImmutableList();

    public int Port { get; }

    public OtlpCollectorServer()
    {
        Port = GetAvailablePort();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/v1/traces/");
        _listener.Start();

        Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    await HandleRequest(context);
                    context.Response.Close();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        });
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        using var ms = new MemoryStream();
        await context.Request.InputStream.CopyToAsync(ms);
        var request = ExportTraceServiceRequest.Parser.ParseFrom(ms.ToArray());
        foreach (var resourceSpan in request.ResourceSpans)
        {
            _receivedSpans.Add(resourceSpan);
        }

        context.Response.StatusCode = 200;
        new ExportTraceServiceResponse().WriteTo(context.Response.OutputStream);
        await context.Response.OutputStream.FlushAsync();
    }
    
    public static OtlpCollectorServer Start()
    {
        return new();
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

// public class OtlpCollectorServer : IAsyncDisposable
// {
//     private readonly Server _server;
//     private readonly OtlpCollector _collectorImpl = new();
//
//     private OtlpCollectorServer()
//     {
//         var serverPort = new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure);
//         _server = new Server
//         {
//             Services = { TraceService.BindService(_collectorImpl) },
//             Ports = { serverPort }
//         };
//         _server.Start();
//         
//         // Get the dynamic port
//         Port = _server.Ports.First().BoundPort;
//     }
//     
//     public int Port { get; }
//
//     public IReadOnlyList<ExportTraceServiceRequest> ReceivedSpans => _collectorImpl.ReceivedSpans;
//
//     public static OtlpCollectorServer Start()
//     {
//         return new OtlpCollectorServer();
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         await _server.ShutdownAsync();
//     }
//     
//     private class OtlpCollector : TraceService.TraceServiceBase
//     {
//         public List<ExportTraceServiceRequest> ReceivedSpans { get; } = new();
//
//         public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
//         {
//             ReceivedSpans.Add(request);
//             Console.WriteLine("Received Trace");
//             return Task.FromResult(new ExportTraceServiceResponse());
//         }
//     } 
// }