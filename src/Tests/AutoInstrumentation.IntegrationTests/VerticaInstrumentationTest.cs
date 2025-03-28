
#if NET5_0_OR_GREATER

using Testcontainers.MsSql;
using System.Data.SqlClient;
using System.Diagnostics;
using AutoInstrumentation.IntegrationTests.Utils;
using Dapper;
using FluentAssertions;
using FluentAssertions.Extensions;
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
    public async Task DbSpanIsCreated()
    {
        using var instrument = new AutoInstrumentor().Instrument();

        await using var connection = new VerticaConnection(_verticaContainer.GetConnectionString());
        connection.Open();

        await connection.QueryAsync("select node_name, node_state, node_address, is_primary, is_readonly from nodes");

        Activities.Should().HaveCount(1);
        Activities[0].OperationName.Should().Be("master");
        Activities[0].Kind.Should().Be(ActivityKind.Client);
        Activities[0].Source.Name.Should().Be("OpenTelemetry.Instrumentation.Vertica");
        Activities[0].Tags.Should().ContainKey("db.statement");
        var dbStatement = Activities[0].Tags.Single(x => x.Key == "db.statement").Value;
        dbStatement.Should().Be("select node_name, node_state, node_address, is_primary, is_readonly from nodes");
    }
    
}
#endif