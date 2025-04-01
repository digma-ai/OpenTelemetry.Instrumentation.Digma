
#if NET47

using Testcontainers.MsSql;
using System.Data.SqlClient;
using System.Diagnostics;
using AutoInstrumentation.IntegrationTests.Utils;
using Dapper;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Digma;

namespace AutoInstrumentation.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public class MssqlInstrumentationTest : BaseInstrumentationTest
{
    private static MsSqlContainer _msSqlContainer;
    
    [ClassInitialize]
    public static async Task Init(TestContext ctx)
    {
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
            .Build();
        await _msSqlContainer.StartAsync();
    }
    
    [ClassCleanup]
    public static async Task Cleanup()
    {
        await _msSqlContainer.DisposeAsync();
    }

    [TestMethod]
    public async Task DbSpanHasSqlStatement()
    {
        using var instrument = new AutoInstrumentor().Instrument();
        
        using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
        connection.Open();

        await connection.QueryAsync("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'");

        Activities.Should().HaveCount(1);
        Activities[0].OperationName.Should().Be("master");
        Activities[0].Kind.Should().Be(ActivityKind.Client);
        Activities[0].Source.Name.Should().Be("OpenTelemetry.Instrumentation.SqlClient");
        Activities[0].Tags.Should().ContainKey("db.statement");
        var dbStatement = Activities[0].Tags.Single(x => x.Key == "db.statement").Value;
        dbStatement.Should().Be("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'");
    }
    
}
#endif