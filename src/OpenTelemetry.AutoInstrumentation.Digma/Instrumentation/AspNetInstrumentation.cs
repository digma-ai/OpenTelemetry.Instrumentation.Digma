using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace OpenTelemetry.AutoInstrumentation.Digma.Instrumentation;

public class AspNetInstrumentation : IDisposable
{
    private readonly DiagnosticSubscriber _observer = new();

    public void Instrument()
    {
        try
        {
            _observer.Subscribe();
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to subscribe DiagnosticSubscriber", e);
        }
    }

    public void Dispose()
    {
        _observer.Dispose();
    }

    private class DiagnosticSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly HttpEndpointDiagnosticObserver _diagnosticObserver = new();
        private readonly List<IDisposable> _subscriptions = new();
        
        public void Subscribe()
        {
            var subscription = DiagnosticListener.AllListeners.Subscribe(this);
            _subscriptions.Add(subscription);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if(value.Name != "Microsoft.AspNetCore")
                return;

            try
            {
                var subscription = value.Subscribe(_diagnosticObserver);
                _subscriptions.Add(subscription);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to subscribe HttpEndpointDiagnosticObserver", e);
            }
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
    
    private class HttpEndpointDiagnosticObserver : IObserver<KeyValuePair<string, object?>>
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
            try
            {
                var httpContext = (HttpContext) pair.Value;
                if (httpContext == null)
                    return;

                var endpointFeature = httpContext.Features?.Get<IEndpointFeature>();
                var descriptor = endpointFeature?.Endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (descriptor == null)
                    return;

                var classFullName = descriptor.ControllerTypeInfo.FullName;
                var methodName = descriptor.MethodInfo.Name;
                var methodParams = DigmaSemanticConventions.BuildMethodParameterTypes(descriptor.MethodInfo);

                Activity.Current?.SetTag(DigmaSemanticConventions.CodeNamespace, classFullName);
                Activity.Current?.SetTag(DigmaSemanticConventions.CodeFunction, methodName);
                Activity.Current?.SetTag(DigmaSemanticConventions.CodeFunctionParameterTypes, methodParams);
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to enrich endpoint activity", e);
            }
        }
    }
}