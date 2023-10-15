using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.Digma.Diagnostic;

namespace OpenTelemetry.Instrumentation.Digma;

public class DiagnosticInit : IHostedService
{
    private readonly DiagnosticSubscriber _observer;

    public DiagnosticInit(IEnumerable<IDigmaDiagnosticObserver> diagnosticObserver)
    {
        _observer = new DiagnosticSubscriber(diagnosticObserver);
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


    private class DiagnosticSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IEnumerable<IDigmaDiagnosticObserver> _diagnosticObservers;
        private readonly List<IDisposable> _subscriptions;

        public DiagnosticSubscriber(IEnumerable<IDigmaDiagnosticObserver> diagnosticObservers)
        {
            _diagnosticObservers = diagnosticObservers;
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
            var diagnosticObserver = _diagnosticObservers.FirstOrDefault(o => o.CanHandle(value.Name));
            if (diagnosticObserver == null) return;
            var subscription = value.Subscribe(diagnosticObserver);
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
}


