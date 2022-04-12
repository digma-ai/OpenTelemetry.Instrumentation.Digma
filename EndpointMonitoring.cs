using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenTelemetry.Instrumentation.Digma;

public static class EndpointMonitoring
{
    public static IServiceCollection AddEndpointMonitoring(this IServiceCollection services)
    {
        services.AddHostedService<DiagnosticInit>();
        return services;
    }

    private class DiagnosticInit : IHostedService
    {
        private readonly DiagnosticSubscriber _observer;

        public DiagnosticInit()
        {
            _observer = new DiagnosticSubscriber();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _observer.Subscribe();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _observer.Dispose();
            return Task.CompletedTask;
        }
    }

    private class DiagnosticSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions;

        public DiagnosticSubscriber()
        {
            _subscriptions = new List<IDisposable>();
        }

        public void Subscribe()
        {
            var subscription = DiagnosticListener.AllListeners.Subscribe(this);
            _subscriptions.Add(subscription); // Created first, and disposed last
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name != "Microsoft.AspNetCore")
                return;

            var subscription = value.Subscribe(new DiagnosticObserver());
            _subscriptions.Add(subscription);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }

    private class DiagnosticObserver : IObserver<KeyValuePair<string, object?>>
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

            Activity.Current?.AddTag("endpoint.type_full_name", descriptor.MethodInfo.DeclaringType?.ToString());
            Activity.Current?.AddTag("endpoint.function", descriptor.MethodInfo.Name);
        }
    }
}