using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Instrumentation.Digma;
using OpenTelemetry.Instrumentation.Digma.Diagnostic;

namespace Digma.MassTransit.Integration;


public static class MassTransitDiagnosticObserverExtensions
{
    public static IServiceCollection UseDigmaMassTransitDiagnosticObserver(this IServiceCollection serviceCollection, Action<MassTransitDigmaConfigurationObserver> action)
    {
        
        var config = new MassTransitDigmaConfigurationObserver();
        action(config);
        serviceCollection.AddSingleton<IMassTransitDigmaConfigurationObserver>(config);
        serviceCollection.AddTransient<IDigmaDiagnosticObserver, MasstransitDiagnosticObserver>();
        serviceCollection.AddEndpointMonitoring();
        return serviceCollection;
    }
}


public interface IMassTransitDigmaConfigurationObserver
{
    MethodInfo? GetConsumerMethodInfo(Type messageType);
}

public class MassTransitDigmaConfigurationObserver: IMassTransitDigmaConfigurationObserver
{
    private readonly Dictionary<Type, MethodInfo> _consumeMethodMap = new();
    private const string ConsumeMethodName = "Consume";

    public MethodInfo?  GetConsumerMethodInfo(Type messageType)
    {
        return _consumeMethodMap.TryGetValue(messageType, out var mi) ? mi : null;
    }
    
    public MassTransitDigmaConfigurationObserver Observe<TConsumer>()
    {
        var consumerType = typeof(TConsumer);
        var iInterfaceType = consumerType.GetInterface(typeof(IConsumer<>).Name);
        if (iInterfaceType is null) throw new InvalidOperationException($"cannot observe consumer of type {consumerType}");

        var genericArguments = iInterfaceType.GetGenericArguments();
        if (genericArguments.Length != 1)  throw new InvalidOperationException($"cannot observe consumer of type {consumerType}");
        
        var messageType = GetMessageType(genericArguments[0]);
        var consumeMethodInfo = GetConsumeMethodInfo(consumerType, iInterfaceType);
        _consumeMethodMap.Add(messageType, consumeMethodInfo );
        return this;
    }

    private static Type GetMessageType(Type messageType)
    {
        if (!messageType.IsGenericType) return messageType;
        var genericTypeDefinition = messageType.GetGenericTypeDefinition();
        if (genericTypeDefinition != typeof(global::MassTransit.Batch<>)) return messageType;
        var arguments = messageType.GetGenericArguments();
        messageType = arguments.Single();
        return messageType;
    }

    private static MethodInfo GetConsumeMethodInfo(Type consumerType, Type iInterfaceType)
    {
        var consumeMethodInfo = consumerType.GetMethods()
            .SingleOrDefault(o => o.ToString() == iInterfaceType.GetMethod(ConsumeMethodName)!.ToString());
        if (consumeMethodInfo == null)
            throw new InvalidOperationException(
                $"cannot observe consumer of type {consumerType}, cannot find consume method info");
        return consumeMethodInfo;
    }
}
