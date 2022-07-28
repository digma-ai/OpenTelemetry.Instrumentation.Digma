using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace OpenTelemetry.Instrumentation.Digma.Diagnostic;

public static class HttpDiagnosticObserverExtensions
{
    public static IServiceCollection UseDigmaHttpDiagnosticObserver(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IDigmaDiagnosticObserver, HttpEndpointDiagnosticObserver>();
        serviceCollection.AddHostedService<DiagnosticInit>();
        return serviceCollection;
    }
}

public class HttpEndpointDiagnosticObserver : IDigmaDiagnosticObserver
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> pair)
    {
        if (pair.Key != "Microsoft.AspNetCore.Routing.EndpointMatched")
            return;
        
        var context = (HttpContext) pair.Value;
        var endpoint = context?.GetEndpoint();
        var descriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (descriptor == null)
            return;
        SpanUtils.AddCommonTags(descriptor.MethodInfo, Activity.Current);
    }

    public bool CanHandle(string diagnosticListener)
    {
        return diagnosticListener == "Microsoft.AspNetCore";
    }
}