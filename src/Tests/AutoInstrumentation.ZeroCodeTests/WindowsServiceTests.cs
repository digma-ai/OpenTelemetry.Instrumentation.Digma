﻿using AutoInstrumentation.ZeroCodeTests.OsServices;
using AutoInstrumentation.ZeroCodeTests.OtelCollector;
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
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WindowsServiceSampleApp\bin\Debug\net9.0\AutoInstrumentation.WindowsServiceSampleApp.exe");
        _windowsServiceManager.CreateService(_serviceName, exeFilePath);
        _windowsServiceManager.InstrumentService(_serviceName, OtelCollectorInitializer.Port, "grpc");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if(_windowsServiceManager.ServiceExists(_serviceName))
            _windowsServiceManager.DeleteService(_serviceName);
    }

    [TestMethod]
    public void Sanity()
    {
        _windowsServiceManager.IsServiceRunning(_serviceName).Should().BeTrue();
        Thread.Sleep(10.Seconds());
        
        
        var channel = GrpcChannel.ForAddress($"http://localhost:{OtelCollectorInitializer.Port}");
        var client = new TraceService.TraceServiceClient(channel);

        // Send an empty trace export request just to test
        var request = new ExportTraceServiceRequest();
        var response = client.Export(request);
        Console.WriteLine(response);
    }
}