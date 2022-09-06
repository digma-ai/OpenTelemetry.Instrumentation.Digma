using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        "OpenTelemetry.Instrumentation.Digma.Tests.Stubs.IDecoratedService";

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

    [TestInitialize]
    public void SetupOtel()
    {
        Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "test", serviceVersion: "2.2"))
            .Build();
    }

    [TestMethod]
    public async Task Activity_Created_For_Async_Attribute_Marked_Method()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        await tracingDecorator.AsyncMethodExplicitlyMarkedForTracing(() =>
        {
            Assert.IsNotNull(Activity.Current);
            AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn,
                "AsyncMethodExplicitlyMarkedForTracing", "Action");
        });
    }

    [TestMethod]
    public void Activity_Created_MethodWithStrangeParams1()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        int intVal = 5;
        tracingDecorator.MethodWithStrangeParams1(() =>
            {
                Assert.IsNotNull(Activity.Current);
                AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn, "MethodWithStrangeParams1",
                    "Action|IList`1[]|ISet`1|IDictionary`2|Int32&");
            },
            new List<string>[] { }, new HashSet<int[]>(), new Dictionary<int, ICollection<string>>(), ref intVal
        );
    }

    [TestMethod]
    public void Activity_Created_MethodJaggedAndMultiDimArraysParams()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        string strVal;
        tracingDecorator.MethodJaggedAndMultiDimArraysParams(() =>
            {
                Assert.IsNotNull(Activity.Current);
                AssertHasCommonTags(Activity.Current, ServiceInterfaceFqn, "MethodJaggedAndMultiDimArraysParams",
                    "Action|String&|Boolean[][][]|Int16[,,][,][,,,]|Int64[][,][][,,]");
            },
            out strVal, new bool[][][] { }, new short[,,,][,][,,] { }, new long[,,][][,][] { }
        );
    }

    [TestMethod]
    public void Activity_Not_Created_For_Non_Attribute_Marked_Method_If_All_Methods_False()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator =
            TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods: false);
        tracingDecorator.MethodNotExplicitlyMarkedForTracing(() => { Assert.IsNull(Activity.Current); });
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
}