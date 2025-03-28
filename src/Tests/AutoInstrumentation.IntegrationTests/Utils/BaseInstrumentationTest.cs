using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AutoInstrumentation.IntegrationTests.Utils;

[TestClass]
public abstract class BaseInstrumentationTest
{
    private MockProcessor _mockProcessor;
    private IDisposable _tracerProvider;

    [TestInitialize]
    public void Init()
    {
        _mockProcessor = new MockProcessor();
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("AutoInstrumentation Testing")
                .AddAttributes(new []{new KeyValuePair<string, object>("code.namespace.root", "AutoInstrumentation")}))
            .AddSqlClientInstrumentation(x => x.SetDbStatementForText=true)
            .AddProcessor(_mockProcessor)
            .AddSource("*")
            .Build();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _tracerProvider.Dispose();
    }

    public IReadOnlyList<Activity> Activities => _mockProcessor.Activities;
    
    private class MockProcessor : BaseProcessor<Activity>
    {
        private readonly List<Activity> _activities = new();

        public override void OnEnd(Activity data)
        {
            _activities.Add(data);
            base.OnEnd(data);
        }

        public IReadOnlyList<Activity> Activities => _activities;

        public void Reset() => _activities.Clear();
    }
}
