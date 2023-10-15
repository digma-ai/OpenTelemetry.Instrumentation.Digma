namespace OpenTelemetry.Instrumentation.Digma.Diagnostic;
public interface IDigmaDiagnosticObserver : IObserver<KeyValuePair<string, object?>>
{
    bool CanHandle(string diagnosticListener);
}