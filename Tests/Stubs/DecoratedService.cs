using System.Diagnostics;
using OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;

namespace OpenTelemetry.Instrumentation.Digma.Tests.Stubs;

public interface IDecoratedService
{
    public void MethodExplicitlyMarkedForTracing(Action stateValidation);

    public Task AsyncMethodExplicitlyMarkedForTracing(Action stateValidation);

    public void MethodNotExplicitlyMarkedForTracing(Action stateValidation);
}

public class DecoratedService : IDecoratedService
{
    [TraceActivity()]
    public void MethodExplicitlyMarkedForTracing(Action stateValidation)
    {
        var v = Activity.Current;
        stateValidation();
    }

    [TraceActivity()]
    public async Task AsyncMethodExplicitlyMarkedForTracing(Action stateValidation)
    {
        stateValidation();
    }

    public void MethodNotExplicitlyMarkedForTracing(Action stateValidation)
    {
        stateValidation();
    }
}