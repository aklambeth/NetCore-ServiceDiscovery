using DnsClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using Polly;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
           
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            // create service collection
            var serviceCollection = new ServiceCollection();
            serviceCollection.Configure<Configuration.ServiceDiscovery>(configuration.GetSection("ServiceDiscovery"));
            ConfigureServices(serviceCollection, configuration);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // entry to run app
            var host = new HostService();
            host.AsyncPolicy = ConfigurePolicys();
            var t = host.OnStarting(serviceProvider.GetService<Process>().Run).GetAwaiter();

            while (!t.IsCompleted); // Don't exit until processes have been halted

        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = configuration.GetSection("ServiceDiscovery").Get<Configuration.ServiceDiscovery>();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IDnsQuery>(p =>
            {
                return new LookupClient(new IPEndPoint[] { config.Consul.DnsEndpoint.ToIPEndPoint() });
            });

            services.AddLogging(configure => configure.AddConsole()).AddTransient<Process>();
            services.AddSingleton<Process>();
        }

        private static AsyncPolicy ConfigurePolicys()
        {
            var circuitBreaker = Policy.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
               onBreak: (ex, timespan) =>
               {
                   Console.WriteLine("░▄███▄░░██░██░░░██▄░██░░▄███▄░░████░▄███▄░██");
                   Console.WriteLine("██▀░▀██░██▄██░░░███▄██░██▀░▀██░██▄░░▀█▄▀▀░██");
                   Console.WriteLine("██▄░▄██░██▀██░░░██▀███░██▄░▄██░██▀░░▄▄▀█▄░▀▀");
                   Console.WriteLine("░▀███▀░░██░██░░░██░░██░░▀███▀░░████░▀███▀░██");
                   Console.WriteLine(ex.Message);
               },
               onReset: () =>
               {

               }
           );

            var fallback = Policy
                .Handle<Polly.CircuitBreaker.BrokenCircuitException>()
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5));

            return fallback.WrapAsync(circuitBreaker);
        }
    }
}
