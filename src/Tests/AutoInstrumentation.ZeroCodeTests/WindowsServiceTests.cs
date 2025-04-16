using System.Diagnostics;
using AutoInstrumentation.ZeroCodeTests.OsServices;
using AutoInstrumentation.ZeroCodeTests.OtelCollector;
using AutoInstrumentation.ZeroCodeTests.Utils;
using FluentAssertions;
using FluentAssertions.Extensions;
using Grpc.Net.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Proto.Collector.Trace.V1;


namespace AutoInstrumentation.ZeroCodeTests;

[TestClass]
public class WindowsServiceTests
{
    private readonly WindowsServiceManager _windowsServiceManager = new();
    private readonly string _serviceName = "ZeroCodeTestingApp" + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss");

    [TestInitialize]
    public void Init()
    {
        Console.WriteLine(_serviceName);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if(_windowsServiceManager.ServiceExists(_serviceName))
            _windowsServiceManager.DeleteService(_serviceName);
    }
    
    [TestMethod]
    public async Task Sanity_Net8()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WindowsServiceSampleApp\bin\Debug\net8.0\AutoInstrumentation.WindowsServiceSampleApp.exe");
        await RunSanity(exeFilePath);
    }
                
    [TestMethod]
    public async Task Sanity_Net9()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WindowsServiceSampleApp\bin\Debug\net9.0\AutoInstrumentation.WindowsServiceSampleApp.exe");
        await RunSanity(exeFilePath);
    }
    
    private async Task RunSanity(string exeFilePath)
    {
        var port = PortUtils.GetAvailablePort();
        
        _windowsServiceManager.CreateService(_serviceName, exeFilePath, port);
        _windowsServiceManager.InstrumentService(_serviceName, 
            $"http://localhost:{OtelCollectorInitializer.Port}/v1/traces/" , 
            "http/protobuf");
        
        var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:{port}");
        response.EnsureSuccessStatusCode();
        
        Retry.Do(() =>
        {
            var spans = OtelCollectorInitializer.GetSpans(_serviceName);
            
            var span = spans.FirstOrDefault(x => x.Scope.Name == "UsersRepository" && x.Span.Name == "GetAllUsers")?.Span;
            
            span.Should().NotBeNull();
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.WindowsServiceSampleApp.UsersRepository");
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetAllUsers");
            
            span.Attributes.Should().NotContain(x => x.Key == "code.function.parameter.types");
            
            var span2 = spans.FirstOrDefault(x => x.Scope.Name == "Microsoft.AspNetCore" && x.Span.Name == "GET")?.Span;
            
            span2.Should().NotBeNull();
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.WindowsServiceSampleApp.Controllers.HomeController");
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetOk");
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function.parameter.types" &&
                x.Value.StringValue == "Int32");
        });
    }
}