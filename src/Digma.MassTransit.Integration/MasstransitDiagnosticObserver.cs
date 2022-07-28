using System.Collections.Concurrent;
using System.Reflection;
using OpenTelemetry.Instrumentation.Digma;
using OpenTelemetry.Instrumentation.Digma.Diagnostic;

namespace Digma.MassTransit.Integration;

public class MasstransitDiagnosticObserver : IDigmaDiagnosticObserver
{
    private readonly IMassTransitDigmaConfigurationObserver _configuration;
    private readonly ConcurrentDictionary<Type, MethodInfo?> _consumerMethodInfoMap = new();
    public MasstransitDiagnosticObserver(IMassTransitDigmaConfigurationObserver configuration, IServiceProvider serviceCollection)
    {
        _configuration = configuration;
    }
    public bool CanHandle(string diagnosticListener)
    {
        return diagnosticListener == "MassTransit";
    }
        
    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key != "MassTransit.Consumer.Consume.Start" || value.Value is null) return;
        var type = value.Value.GetType();
        var methodInfo = _consumerMethodInfoMap.GetOrAdd(value.Value.GetType(), t =>
        {
            if (!type.IsGenericType)
                return null;

            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 1) return null;
            var messageType = genericArguments[0];
            return _configuration.GetConsumerMethodInfo(messageType);
        });
        if (methodInfo is null) return;
        
        //Parent span is of kind consumer
        SpanUtils.AddCommonTags(methodInfo,System.Diagnostics.Activity.Current?.Parent);
    }
}
