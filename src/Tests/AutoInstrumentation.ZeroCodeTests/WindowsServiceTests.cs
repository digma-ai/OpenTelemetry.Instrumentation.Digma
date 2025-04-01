using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AutoInstrumentation.ZeroCodeTests;

[TestClass]
public class WindowsServiceTests
{
    private readonly WindowsServiceManager _windowsServiceManager = new();
    private readonly string _serviceName = "ZeroCodeTestingApp" + DateTime.Now.ToString("_yyyy_MM_d_HH_mm_ss");
    
    [TestInitialize]
    public void Init()
    {
        Console.WriteLine(_serviceName);
        var exeFilePath = Path.GetFullPath(@"..\..\..\..\AutoInstrumentation.WindowsServiceSampleApp\bin\Debug\net9.0\AutoInstrumentation.WindowsServiceSampleApp.exe");
        _windowsServiceManager.CreateService(_serviceName, exeFilePath);
        _windowsServiceManager.InstrumentService(_serviceName);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if(_windowsServiceManager.ServiceExists(_serviceName))
            _windowsServiceManager.DeleteService(_serviceName);
    }

    [TestMethod]
    [Ignore]
    public void Sanity()
    {
        _windowsServiceManager.IsServiceRunning(_serviceName).Should().BeTrue();
    }
}