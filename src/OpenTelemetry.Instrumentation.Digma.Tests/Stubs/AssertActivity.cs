using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenTelemetry.Instrumentation.Digma.Tests.Stubs;

public static class AssertActivity
{
    public static void SpanNameIs(string name, Activity activity)
    {
        Assert.AreEqual(name, activity.OperationName);
    }
    
    public static void InstrumentationScopeIs(string name, Activity activity)
    {
        Assert.AreEqual(name, activity.Source.Name);
    }
    
    public static void DurationIs(TimeSpan value, TimeSpan delta, Activity activity)
    {
        Assert.AreEqual(value.TotalMilliseconds, activity.Duration.TotalMilliseconds, delta.TotalMilliseconds);
    }
    
    public static void HasTag(string key, string value, Activity activity)
    {
        CollectionAssert.Contains(activity.Tags.ToArray(), new KeyValuePair<string, string>(key, value));
    }
}