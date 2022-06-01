using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Instrumentation.Digma.Helpers;
using OpenTelemetry.Instrumentation.Digma.Tests.Stubs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.Digma.Tests;

[TestClass]
public class TestTracingDecorator
{
    
    [TestMethod]
    public void Activity_Created_For_Attribute_Marked_Method()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service);
        tracingDecorator.MethodExplicitlyMarkedForTracing(() =>
        {
            Assert.IsNotNull(Activity.Current);
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
        });
    }

    [TestMethod]
    public void Activity_Not_Created_For_Non_Attribute_Marked_Method_If_All_Methods_False()
    {
        DecoratedService service = new DecoratedService();
        IDecoratedService tracingDecorator = TraceDecorator<IDecoratedService>.Create(service, decorateAllMethods:false);
        tracingDecorator.MethodNotExplicitlyMarkedForTracing(() =>
        {
            
            Assert.IsNull(Activity.Current);
        });
    }

    
}