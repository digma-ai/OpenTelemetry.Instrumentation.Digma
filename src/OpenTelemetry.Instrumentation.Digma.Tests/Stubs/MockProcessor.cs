using System.Collections.Generic;
using System.Diagnostics;

namespace OpenTelemetry.Instrumentation.Digma.Tests.Stubs;

public class MockProcessor : BaseProcessor<Activity>
{
    private readonly List<Activity> _activities = new();

    public override void OnEnd(Activity data)
    {
        _activities.Add(data);
        base.OnEnd(data);
    }

    public IReadOnlyList<Activity> Activities => _activities.AsReadOnly();

    public void Reset() => _activities.Clear();
}