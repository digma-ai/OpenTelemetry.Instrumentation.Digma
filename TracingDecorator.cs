using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.Trace;

namespace Sample.MoneyTransfer.API.Utils;

public class TraceDecorator<TDecorated> : DispatchProxy
{

    private readonly ActivitySource _activity = new(nameof(TDecorated));
    private TDecorated _decorated;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        using var activity = _activity.StartActivity($"{_decorated.GetType().FullName}.{targetMethod.Name}");
        try
        {
            var result = targetMethod.Invoke(_decorated, args);
            return result;
        }
        catch (Exception e)
        {
            activity.RecordException(e);
            throw;

        }
    }

    public static TDecorated Create(TDecorated decorated)
    {
        object proxy = Create<TDecorated, TraceDecorator<TDecorated>>()!;
        ((TraceDecorator<TDecorated>)proxy!).SetParameters(decorated);

        return (TDecorated)proxy;
    }

    private void SetParameters(TDecorated decorated)
    {
        _decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
    }
}