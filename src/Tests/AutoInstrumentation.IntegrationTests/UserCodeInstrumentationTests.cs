
using System.Diagnostics;
using AutoInstrumentation.IntegrationTests.Utils;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Digma;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace AutoInstrumentation.IntegrationTests;

[TestClass]
public class UserCodeInstrumentationTests : BaseInstrumentationTest
{
    [TestMethod]
    public void Sanity()
    {
        using var instrument = new AutoInstrumentor().Instrument();

        new AutoInstrumentor(new Configuration
        {
            Include = new[]
            {
                new InstrumentationRule
                {
                    Namespaces = "AutoInstrumentation*",
                    Methods = "MyMethod"
                }
            }
        }).Instrument();

        MyMethod();
        
        Activities.Should().HaveCount(1);
        Activities[0].OperationName.Should().Be("MyMethod");
        Activities[0].Source.Name.Should().Be("UserCodeInstrumentationTests");
        Activities[0].Kind.Should().Be(ActivityKind.Internal);
        Activities[0].Status.Should().Be(ActivityStatusCode.Ok);
        Activities[0].Tags.Should().Contain(
            new KeyValuePair<string, string?>("digma.instrumentation.extended.package", "AutoInstrumentation.IntegrationTests"),
            new KeyValuePair<string, string?>("code.namespace", "AutoInstrumentation.IntegrationTests.UserCodeInstrumentationTests"),
            new KeyValuePair<string, string?>("code.function", "MyMethod")
        );
    }
    
    [TestMethod]
    public void Error()
    {
        using var instrument = new AutoInstrumentor().Instrument();

        new AutoInstrumentor(new Configuration
        {
            Include = new[]
            {
                new InstrumentationRule
                {
                    Namespaces = "AutoInstrumentation*",
                    Methods = "ErroredMethod"
                }
            }
        }).Instrument();

        try
        {
            ErroredMethod();
        }
        catch{}
        
        Activities.Should().HaveCount(1);
        Activities[0].Status.Should().Be(ActivityStatusCode.Error);
        Activities[0].Events.Should().HaveCount(1);
        
        var errEvent = Activities[0].Events.Single();
        errEvent.Name.Should().Be("exception");
        errEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.type", "System.InvalidOperationException"));
        errEvent.Tags.Should().Contain(new KeyValuePair<string, object?>("exception.message", "ERR"));
        errEvent.Tags.Should().ContainKey("exception.stacktrace");
    }

    private void MyMethod()
    {
        
    }

    private void ErroredMethod()
    {
        throw new InvalidOperationException("ERR");
    }
}