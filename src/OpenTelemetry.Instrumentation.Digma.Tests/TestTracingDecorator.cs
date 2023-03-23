using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Instrumentation.Digma.Helpers;
using OpenTelemetry.Instrumentation.Digma.Tests.Stubs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.Digma.Tests;

[TestClass]
public class TestTracingDecorator
{
    private static readonly string ServiceInterfaceFqn =
        "OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService";

    [TestMethod]
    public void Activity_Created_For_Attribute_Marked_Method()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        tracingDecorator.MethodExplicitlyMarkedForTracing(() =>
        {
            Assert.IsNotNull(Activity.Current);
            AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn,
                "MethodExplicitlyMarkedForTracing", "Action");
        });
    }
    
    [TestMethod]
    public void Attributes_Injected_To_Marked_Method()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        tracingDecorator.MethodExplicitlyMarkedForTracingWithAttributes(() =>
        {
            Assert.IsNotNull(Activity.Current);
            AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn,
                "MethodExplicitlyMarkedForTracingWithAttributes", "Action");
            AssertHasTag(Activity.Current, "att1", "value1");

        });
    }
    private MockProcessor _mockProcessor = new ();
    
    // [TestMethod]
    // public void Activity_Created_For_Attribute_Marked_Method()
    // {
    //     DecoratedService service = new DecoratedService();
    //     IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
    //     tracingDecorator.MethodExplicitlyMarkedForTracing(() =>
    //     {
    //         Assert.IsNotNull(Activity.Current);
    //         AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn,
    //             "MethodExplicitlyMarkedForTracing", "Action");
    //     });
    // }

    [TestInitialize]
    public void SetupOtel()
    {
        _mockProcessor.Reset();
        Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "test", serviceVersion: "2.2"))
            .AddProcessor(_mockProcessor)
            .Build();
    }

    // [TestMethod]
    // public async Task Activity_Created_For_Async_Attribute_Marked_Method()
    // {
    //     DecoratedService service = new DecoratedService();
    //     IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
    //     await tracingDecorator.AsyncMethodExplicitlyMarkedForTracing(() =>
    //     {
    //         Assert.IsNotNull(Activity.Current);
    //         AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn,
    //             "AsyncMethodExplicitlyMarkedForTracing", "Action");
    //     });
    // }
    //
    // [TestMethod]
    // public void Activity_Created_MethodWithStrangeParams1()
    // {
    //     DecoratedService service = new DecoratedService();
    //     IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
    //     int intVal = 5;
    //     tracingDecorator.MethodWithStrangeParams1(() =>
    //         {
    //             Assert.IsNotNull(Activity.Current);
    //             AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn, "MethodWithStrangeParams1",
    //                 "Action|IList`1[]|ISet`1|IDictionary`2|Int32&");
    //         },
    //         new List<string>[] { }, new HashSet<int[]>(), new Dictionary<int, ICollection<string>>(), ref intVal
    //     );
    // }
    //
    // [TestMethod]
    // public void Activity_Created_MethodJaggedAndMultiDimArraysParams()
    // {
    //     DecoratedService service = new DecoratedService();
    //     IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
    //     string strVal;
    //     tracingDecorator.MethodJaggedAndMultiDimArraysParams(() =>
    //         {
    //             Assert.IsNotNull(Activity.Current);
    //             AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn, "MethodJaggedAndMultiDimArraysParams",
    //                 "Action|String&|Boolean[][][]|Int16[,,][,][,,,]|Int64[][,][][,,]");
    //         },
    //         out strVal, new bool[][][] { }, new short[,,,][,][,,] { }, new long[,,][][,][] { }
    //     );
    // }
    //
    // [TestMethod]
    // public void Activity_Not_Created_For_Non_Attribute_Marked_Method_If_All_Methods_False()
    // {
    //     DecoratedService service = new DecoratedService();
    //     IDecoratedService tracingDecorator =
    //         TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods: false);
    //     tracingDecorator.MethodNotExplicitlyMarkedForTracing(() => { Assert.IsNull(Activity.Current); });
    // }

    [TestMethod]
    public async Task Activity_Async_Void()
    {
        // Arrange
        var service = new DecoratedService();
        var decoratedService = TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods: true);
        
        // Act #1
        await decoratedService.AsyncVoid();
        var activity = _mockProcessor.Activities.Single();
        AssertActivity.SpanNameIs("AsyncVoid", activity);
        AssertActivity.InstrumentationScopeIs("OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
        AssertActivity.DurationIs(100.Milliseconds(), 30.Milliseconds(), activity);
        AssertActivity.HasTag("code.namespace", "OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
        AssertActivity.HasTag("code.function", "AsyncVoid", activity);
    }
    
    [TestMethod]
    public async Task Activity_Async_Value()
    {
        // Arrange
        var service = new DecoratedService();
        var decoratedService = TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods: true);
        
        // Act #1
        var result = await decoratedService.AsyncValue();
        Assert.AreEqual(123, result);
        
        var activity = _mockProcessor.Activities.Single();
        AssertActivity.SpanNameIs("AsyncValue", activity);
        AssertActivity.InstrumentationScopeIs("OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
        AssertActivity.DurationIs(100.Milliseconds(), 30.Milliseconds(), activity);
        AssertActivity.HasTag("code.namespace", "OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
        AssertActivity.HasTag("code.function", "AsyncValue", activity);
    }
    
    [TestMethod]
    public async Task Activity_Async_Error()
    {
        // Arrange
        var service = new DecoratedService();
        var decoratedService = TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods: true);
        
        // Act #1
        try
        {
            var task = decoratedService.AsyncError();
            await task;
            throw task.Exception!;
        }
        catch (Exception e)
        {
            Assert.AreEqual(e.Message, "Bla");
            
            var activity = _mockProcessor.Activities.Single();
            AssertActivity.SpanNameIs("AsyncError", activity);
            AssertActivity.InstrumentationScopeIs("OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
            AssertActivity.DurationIs(100.Milliseconds(), 30.Milliseconds(), activity);
            AssertActivity.HasTag("code.namespace", "OpenTelemetry.Instrumentation.Digma.Tests.Stubs.DecoratedService", activity);
            AssertActivity.HasTag("code.function", "AsyncError", activity);
        }
    }

    private void AssertHasCommonTags(Activity? activity,
        string expectedClassName, string expectedMethodName, string expectedParameterTypes)
    {
        var kvpTags = activity.Tags.ToArray();
        CollectionAssert.Contains(kvpTags, new KeyValuePair<string, string>("code.namespace", expectedClassName));
        CollectionAssert.Contains(kvpTags, new KeyValuePair<string, string>("code.function", expectedMethodName));
        if (!string.IsNullOrWhiteSpace(expectedParameterTypes))
        {
            CollectionAssert.Contains(kvpTags,
                new KeyValuePair<string, string>("code.function.parameter.types", expectedParameterTypes));
        }
    }
    
    private void AssertHasTag(Activity? activity, string name, string value)
    {
        var kvpTags = activity.Tags.ToArray();
        CollectionAssert.Contains(kvpTags, new KeyValuePair<string, string>(name, value));
    }
}