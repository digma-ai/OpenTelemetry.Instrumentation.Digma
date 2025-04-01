using System.Reflection;
using AutoInstrumentation.WindowsServiceSampleApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
        $"logs\\{Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "general"}_log.txt"))
    .CreateLogger();

try
{
    Log.Information("Creating Host");
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(s =>
        {
            s.AddSingleton<IUsersRepository, UsersRepository>();
            s.AddHostedService<Worker>();
        })
        .UseWindowsService()
        .UseSystemd()
        .Build();

    Log.Information("Running Host");
    host.Run();
    Log.Information("Exited");
}
catch (Exception e)
{
    Log.Error(e, "App crashed with error");
}