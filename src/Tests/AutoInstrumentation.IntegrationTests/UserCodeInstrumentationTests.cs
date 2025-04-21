using System.Diagnostics;
using AutoInstrumentation.IntegrationTests.Utils;
using FluentAssertions;
using FluentAssertions.Extensions;
using OpenTelemetry.AutoInstrumentation.Digma;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace AutoInstrumentation.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public class UserCodeInstrumentationTests : BaseInstrumentationTest
{
    [TestMethod]
    public void Sanity()
    {
        var configuration = new Configuration
        {
            Include = new[]
            {
                new InstrumentationRule
                {
                    Namespaces = "AutoInstrumentation*",
                    Methods = "MyMethod"
                }
            }
        };
        
        using var instrument = new AutoInstrumentor(configuration).Instrument();

        MyMethod();
        
        Activities.Should().HaveCount(1);
        Activities[0].OperationName.Should().Be("MyMethod");
        Activities[0].Source.Name.Should().Be("UserCodeInstrumentationTests");
        Activities[0].Kind.Should().Be(ActivityKind.Internal);
#if !NET5_0
        Activities[0].Status.Should().Be(ActivityStatusCode.Ok);
#endif
        Activities[0].Duration.Should().BeCloseTo(100.Milliseconds(), 50.Milliseconds());
        Activities[0].TagObjects.Should().Contain(
            new KeyValuePair<string, object?>("digma.instrumentation.extended.package", "AutoInstrumentation.IntegrationTests"),
            new KeyValuePair<string, object?>("code.namespace", "AutoInstrumentation.IntegrationTests.UserCodeInstrumentationTests"),
            new KeyValuePair<string, object?>("code.function", "MyMethod")
        );
    }
        
    [TestMethod]
    public void NestedOnly()
    {
        var configuration = new Configuration
        {
            Include = new[]
            {
                new InstrumentationRule
                {
                    Namespaces = "AutoInstrumentation*",
                    Methods = "MyMethod",
                    NestedOnly = true
                }
            }
        };
        
        using var instrument = new AutoInstrumentor(configuration).Instrument();

        MyMethod();
        
        Activities.Should().HaveCount(1);
        Activities[0].TagObjects.Should().Contain(new KeyValuePair<string, object?>("digma.nestedOnly", true));
    }
    
    [TestMethod]
    public void Error()
    {
        var configuration = new Configuration
        {
            Include = new[]
            {
                new InstrumentationRule
                {
                    Namespaces = "AutoInstrumentation*",
                    Methods = "ErroredMethod"
                }
            }
        };
        
        using var instrument = new AutoInstrumentor(configuration).Instrument();

        try
        {
            ErroredMethod();
        }
        catch{}
        
        Activities.Should().HaveCount(1);
#if !NET5_0
        Activities[0].Status.Should().Be(ActivityStatusCode.Error);
#endif
        Activities[0].Events.Should().HaveCount(1);
        
        var errEvent = Activities[0].Events.Single();
        errEvent.Name.Should().Be("exception");
        errEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.type", "System.InvalidOperationException"));
        errEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.message", "ERR"));
        errEvent.Tags.Should().ContainKey("exception.stacktrace");
    }

    private void MyMethod()
    {
        Thread.Sleep(100.Milliseconds());
    }

    private void ErroredMethod()
    {
        throw new InvalidOperationException("ERR");
    }
}