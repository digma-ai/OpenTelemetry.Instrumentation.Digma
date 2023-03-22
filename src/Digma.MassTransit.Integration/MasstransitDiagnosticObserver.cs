using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using MassTransit;
using OpenTelemetry.Instrumentation.Digma;

namespace Digma.MassTransit.Integration;

public class DigmaMassTransitConsumeObserver : IConsumeObserver
{    
    private readonly IMassTransitDigmaConfigurationObserver _configuration;
    private readonly ConcurrentDictionary<Type, MethodInfo?> _consumerMethodInfoMap = new();

    public DigmaMassTransitConsumeObserver(IMassTransitDigmaConfigurationObserver configuration)
    {
        _configuration = configuration;
    }
    
    async Task IConsumeObserver.PreConsume<T>(ConsumeContext<T> context)
    {
        var methodInfo = _consumerMethodInfoMap.GetOrAdd(context.Message.GetType(), type => _configuration.GetConsumerMethodInfo(type));
        if (methodInfo is null) return;
        
        SpanUtils.AddCommonTags(methodInfo.DeclaringType, methodInfo,Activity.Current);

        await Task.CompletedTask;
    }

    Task IConsumeObserver.PostConsume<T>(ConsumeContext<T> context)
    {
        // called after the consumer's Consume method is called
        // if an exception was thrown, the ConsumeFault method is called instead
        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFault<T>(ConsumeContext<T> context, Exception exception)
    {
        // called if the consumer's Consume method throws an exception
        return Task.CompletedTask;
    }
}
/*
 
Filters
class DigmaMassTransitConsumerFilter<T>: IFilter<ConsumerConsumeContext<T>>where T : class
{
    public async Task Send(ConsumerConsumeContext<T> context, IPipe<ConsumerConsumeContext<T>> next)
    {
        await next.Send(context).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
    }
}

class DigmaMassTransitConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IMassTransitDigmaConfigurationObserver _configuration;
    private readonly ConcurrentDictionary<Type, MethodInfo?> _consumerMethodInfoMap = new();

    public DigmaMassTransitConsumeFilter(IMassTransitDigmaConfigurationObserver configuration)
    {
        _configuration = configuration;
    }
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
         var methodInfo = _consumerMethodInfoMap.GetOrAdd(context.Message.GetType(), type =>
         {
             // if (!type.IsGenericType)
             //     return null;
             //
             // var genericArguments = type.GetGenericArguments();
             // if (genericArguments.Length != 1) return null;
             // var messageType = genericArguments[0];
             return _configuration.GetConsumerMethodInfo(type);
         });
         if (methodInfo is null) return;
        
        //Parent span is of kind consumer
        SpanUtils.AddCommonTags(methodInfo,System.Diagnostics.Activity.Current);
        
        await next.Send(context).ConfigureAwait(false);
    }
      
    public void Probe(ProbeContext context) { }
}
*/
/*
MassTransit 7
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
*/