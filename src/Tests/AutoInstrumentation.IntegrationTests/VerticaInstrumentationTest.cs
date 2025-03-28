
#if NET5_0_OR_GREATER

using System.Diagnostics;
using AutoInstrumentation.IntegrationTests.Utils;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Digma;
using Vertica.Data.VerticaClient;

namespace AutoInstrumentation.IntegrationTests;

[TestClass]
public class VerticaInstrumentationTest : BaseInstrumentationTest
{
    private static VerticaContainer _verticaContainer;
    
    [AssemblyInitialize]
    public static async Task Init(TestContext ctx)
    {
        _verticaContainer = new VerticaBuilder().Build();
        await _verticaContainer.StartAsync(new CancellationTokenSource().Token);
    }
    
    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        await _verticaContainer.DisposeAsync();
    }

    [TestMethod]
    public async Task AllExecuteMethodsCreateDbSpans()
    {
        using var instrument = new AutoInstrumentor().Instrument();

        await using var connection = new VerticaConnection(_verticaContainer.GetConnectionString());
        connection.Open();
        
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 1";
            command.ExecuteReader();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 2";
            await command.ExecuteReaderAsync();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 3";
            command.ExecuteScalar();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 4";
            await command.ExecuteScalarAsync();
        }
        
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 5";
            command.ExecuteNonQuery();
        }
        
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "select 6";
            await command.ExecuteNonQueryAsync();
        }

        var activities = Activities.Where(x => x.Kind == ActivityKind.Client).ToArray();
        activities.Should().HaveCount(6);

        for (int i = 0; i < 6; i++)
        {
            var activity = activities[i];
            activity.OperationName.Should().Be("master");
            activity.Kind.Should().Be(ActivityKind.Client);
            activity.Source.Name.Should().Be("OpenTelemetry.Instrumentation.Vertica");
            activity.Tags.Should().Contain(
                new KeyValuePair<string, string?>("db.system", "vertica"),
                new KeyValuePair<string, string?>("db.name", "master"),
                new KeyValuePair<string, string?>("db.statement", $"select {i + 1}")
            );
        }
    }
    
}
#endif