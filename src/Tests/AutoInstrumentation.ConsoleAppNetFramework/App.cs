using System;
using System.Threading;
using Serilog;

namespace AutoInstrumentation.ConsoleAppNetFramework
{
    public class App
    {
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IUsersRepository _usersRepository = new UsersRepository();
    
        public App()
        {
            _thread = new Thread(BackgroundProcess);
        }

        private void BackgroundProcess()
        {
            Log.Information("Worker started");
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var users = _usersRepository.GetAllUsers();
                    Log.Information("Users {u}", users);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cancellationTokenSource.Token)
            {
            }
            catch (Exception e)
            {
                Log.Error(e, "Worker crashed"); 
            }
            Log.Information("Worker finished"); 
        }

        public void Start()
        {
            Log.Information("Starting publisher loop");
            _thread.Start();
            Log.Information("Service started.");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _thread.Join();
            Log.Information("Service stopped.");
        }
    }
}