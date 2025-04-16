using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Serilog;

namespace AutoInstrumentation.ConsoleAppNetFramework
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME");
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                    $"logs\\{serviceName ?? "general"}_log.txt"))
                .CreateLogger();

            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                RunAsWindowsService(serviceName);
            }
            else
            {
                RunAsConsoleApp();
            }
        }
        
        private static void RunAsConsoleApp()
        {
            var app = new App();
            app.Start();
            Console.WriteLine("Press eny key to exit");
            Console.ReadKey();
            app.Stop();
        }

        private static void RunAsWindowsService(string serviceName)
        {
            ServiceBase.Run(new ServiceWrapper(serviceName));
        }

        class ServiceWrapper: ServiceBase
        {
            private readonly App _app = new App();

            public ServiceWrapper(string serviceName)
            {
                ServiceName = serviceName;
            }
        
            protected override void OnStart(string[] args)
            {
                _app.Start();
                base.OnStart(args);
            }

            protected override void OnStop()
            {
                _app.Stop();
                base.OnStop();
            }
        }
    }
}