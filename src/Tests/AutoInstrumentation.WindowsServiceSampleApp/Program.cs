using System.Reflection;
using AutoInstrumentation.WindowsServiceSampleApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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
    
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SERVICE_NAME")))
    {
        builder.Host.UseWindowsService();
        builder.Host.UseSystemd(); 
    }
    builder.Services.AddSingleton<IUsersRepository, UsersRepository>();
    builder.Services.AddHostedService<Worker>();
    
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
        app.UseDeveloperExceptionPage();
    
    app.UseRouting();
    app.MapControllers();
    
    Log.Information("Running Host");
    app.Run();
    Log.Information("Exited");
}
catch (Exception e)
{
    Log.Error(e, "App crashed with error");
}