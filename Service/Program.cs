using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using TestService.Configuration;
using TestService.Helpers;
using Winton.Extensions.Configuration.Consul;

namespace TestService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

            CreateWebHostBuilder(args).UseConfiguration(configuration).Build().Run();

        }

        /// <summary>
        /// Load our configuration settings
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .ConfigureAppConfiguration((hostingContext, builder) =>
                {
                    var config = hostingContext.Configuration.GetSection("ServiceDiscovery").Get<ServiceDiscovery>();
                    builder
                    .AddConsul(
                        $"{hostingContext.HostingEnvironment.ApplicationName}/{hostingContext.HostingEnvironment.EnvironmentAttribute()}/appsettings.json",
                        cancellationTokenSource.Token,
                        options =>
                        {
                            options.ConsulConfigurationOptions =
                                cco => { cco.Address = config.Consul.HttpEndpoint.ToUri(); };
                            options.Optional = true;
                            options.ReloadOnChange = true;
                            options.OnLoadException = exceptionContext => { exceptionContext.Ignore = true; };
                        });
               })
        .UseStartup<Startup>();
        }
    }
}
