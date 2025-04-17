using Microsoft.Extensions.Hosting;
using Serilog;

namespace AutoInstrumentation.ConsoleAppDotNet;

class Worker : BackgroundService
{
    private readonly IUsersRepository _usersRepository;

    public Worker(IUsersRepository usersRepository)
    {
        _usersRepository = usersRepository;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Worker started"); 
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var users = _usersRepository.GetAllUsers();
                Log.Information("Users {u}", users);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException e) when (e.CancellationToken == stoppingToken)
        {
        }
        Log.Information("Worker finished"); 
    }
}