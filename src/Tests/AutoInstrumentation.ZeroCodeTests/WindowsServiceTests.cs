using AutoInstrumentation.ZeroCodeTests.OsServices;
using AutoInstrumentation.ZeroCodeTests.OtelCollector;
using AutoInstrumentation.ZeroCodeTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


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
    public void Sanity_ConsoleApp_NetFramework47()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.ConsoleAppNetFramework\bin\Debug\AutoInstrumentation.ConsoleAppNetFramework.exe");
        _windowsServiceManager.CreateService(_serviceName, exeFilePath, 0);
        _windowsServiceManager.InstrumentService(_serviceName, 
            $"http://localhost:{OtelCollectorInitializer.Port}/v1/traces/" , 
            "http/protobuf");

        Retry.Do(() =>
        {
            var spans = OtelCollectorInitializer.GetSpans(_serviceName);

            var span = spans.FirstOrDefault(x => x.Scope.Name == "UsersRepository" && x.Span.Name == "GetAllUsers")
                ?.Span;

            span.Should().NotBeNull();
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.ConsoleAppNetFramework.UsersRepository");
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetAllUsers");
        });
    }
          
    [TestMethod]
    public void Sanity_ConsoleApp_DotNet8()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.ConsoleAppDotNet\bin\Debug\net8.0\AutoInstrumentation.ConsoleAppDotNet.exe");
        _windowsServiceManager.CreateService(_serviceName, exeFilePath, 0);
        _windowsServiceManager.InstrumentService(_serviceName, 
            $"http://localhost:{OtelCollectorInitializer.Port}/v1/traces/" , 
            "http/protobuf");

        Retry.Do(() =>
        {
            var spans = OtelCollectorInitializer.GetSpans(_serviceName);

            var span = spans.FirstOrDefault(x => x.Scope.Name == "UsersRepository" && x.Span.Name == "GetAllUsers")
                ?.Span;

            span.Should().NotBeNull();
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.ConsoleAppDotNet.UsersRepository");
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetAllUsers");
        });
    }
                 
    [TestMethod]
    public void Sanity_ConsoleApp_DotNet9()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.ConsoleAppDotNet\bin\Debug\net9.0\AutoInstrumentation.ConsoleAppDotNet.exe");
        _windowsServiceManager.CreateService(_serviceName, exeFilePath, 0);
        _windowsServiceManager.InstrumentService(_serviceName, 
            $"http://localhost:{OtelCollectorInitializer.Port}/v1/traces/" , 
            "http/protobuf");

        Retry.Do(() =>
        {
            var spans = OtelCollectorInitializer.GetSpans(_serviceName);

            var span = spans.FirstOrDefault(x => x.Scope.Name == "UsersRepository" && x.Span.Name == "GetAllUsers")
                ?.Span;

            span.Should().NotBeNull();
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.ConsoleAppDotNet.UsersRepository");
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetAllUsers");
        });
    }
         
    [TestMethod]
    public void Sanity_WebApp_Net8()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WebAppDotNet\bin\Debug\net8.0\AutoInstrumentation.WebAppDotNet.exe");
        RunWebAppSanity(exeFilePath);
    }
                
    [TestMethod]
    public void Sanity_WebApp_Net9()
    {
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WebAppDotNet\bin\Debug\net9.0\AutoInstrumentation.WebAppDotNet.exe");
        RunWebAppSanity(exeFilePath);
    }
    
    private void RunWebAppSanity(string exeFilePath)
    {
        var port = PortUtils.GetAvailablePort();
        
        _windowsServiceManager.CreateService(_serviceName, exeFilePath, port);
        _windowsServiceManager.InstrumentService(_serviceName, 
            $"http://localhost:{OtelCollectorInitializer.Port}/v1/traces/" , 
            "http/protobuf");
        
        var client = new HttpClient();

        Retry.Do(() =>
        {
            var response = client.GetAsync($"http://localhost:{port}").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var spans = OtelCollectorInitializer.GetSpans(_serviceName);
            
            var span = spans.FirstOrDefault(x => x.Scope.Name == "UsersRepository" && x.Span.Name == "GetAllUsers")?.Span;
            
            span.Should().NotBeNull();
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.WebAppDotNet.UsersRepository");
            
            span.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetAllUsers");
            
            span.Attributes.Should().NotContain(x => x.Key == "code.function.parameter.types");
            
            var span2 = spans.FirstOrDefault(x => x.Scope.Name == "Microsoft.AspNetCore" && x.Span.Name == "GET")?.Span;
            
            span2.Should().NotBeNull();
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.namespace" &&
                x.Value.StringValue == "AutoInstrumentation.WebAppDotNet.Controllers.HomeController");
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function" &&
                x.Value.StringValue == "GetOk");
            
            span2.Attributes.Should().ContainSingle(x =>
                x.Key == "code.function.parameter.types" &&
                x.Value.StringValue == "Int32");
        });
    }
}