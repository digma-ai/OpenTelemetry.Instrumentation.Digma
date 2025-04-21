using System.Reflection;
using AutoInstrumentation.WebAppDotNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;


AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
        $"logs\\{Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "general"}_log.txt"))
    .CreateLogger();

try
{
    Log.Information("Creating Host");

    var builder = Host.CreateDefaultBuilder(args);
        
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SERVICE_NAME")))
    {
        builder = builder
            .UseWindowsService()
            .UseSystemd();
    }
    
    var app = builder
        .UseSerilog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        })
        .Build();
  
    Log.Information("Running Host");
    app.Run();
    Log.Information("Exited");
}
catch (Exception e)
{
    Log.Error(e, "App crashed with error");
}