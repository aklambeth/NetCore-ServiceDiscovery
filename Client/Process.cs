using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TestClient.Configuration;
using Polly;
using System.Threading;
using Polly.Timeout;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TestClient
{
    public class Process
    {
        private readonly ILogger _logger;
        private readonly IDnsQuery _dns;
        private readonly string _env;
        private readonly IOptions<ServiceDiscovery> _options;
        private readonly IAsyncPolicy<string> noDnsPolicy;
        private readonly IAsyncPolicy httpRequestPolicy;
        private readonly string SERVICE_NAME = "TestService";
        private readonly string BASE_DOMAIN = "service.consul";

        private CancellationToken _cancellation_token;

        public Process(IOptions<ServiceDiscovery> options, ILogger<Process> logger, IDnsQuery dns, IConfiguration config)
        {
            _logger = logger;
            _dns = dns ?? throw new ArgumentNullException(nameof(dns));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _env = ParseEnvironmentName((config.GetValue<string>("ASPNETCORE_ENVIRONMENT")));

            // Define our Fault handling policys

            var timeout = Policy.TimeoutAsync(3, TimeoutStrategy.Pessimistic, onTimeoutAsync: (ctx, timespan, task) => 
            {
                task.ContinueWith(t => 
                {

                    
                    if (t.IsFaulted)
                    {
                        _logger.LogCritical("Timeout");
                    }
                    else if (t.IsCanceled)
                    {
                        _logger.LogCritical("Timeout");
                    }
                });

                return Task.FromException(new DnsResponseException());
            });

            var circuitbreaker = Policy.Handle<HttpRequestException>().CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
                onBreak: (ex, timespan) =>
                {
                    Console.WriteLine(ex.Message);
                },
                onReset: () =>
                {

                }
           );

           noDnsPolicy = Policy<string>
                .Handle<ArgumentNullException>()
                    .Or<InvalidOperationException>()
                    .Or<DnsResponseException>()
                    .WaitAndRetryAsync(10, (t) => TimeSpan.FromSeconds(1), onRetry: (ex, b) => { _logger.LogDebug($"Failed{ ex?.Exception?.Message ?? ""}"); Console.Write("."); })
                    .WrapAsync(timeout);

           httpRequestPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(new[]
                {
                     TimeSpan.FromSeconds(2),
                     TimeSpan.FromSeconds(4),
                     TimeSpan.FromSeconds(8)
                })
                .WrapAsync(Policy.TimeoutAsync(5))
                .WrapAsync(circuitbreaker);
        }

        /// <summary>
        /// Take the name of the current environment from ASPNETCORE_ENVIRONMENT variable and translate to Tag name
        /// </summary>
        /// <param name="name">ASPNETCORE_ENVIRONMENT</param>
        /// <returns>Environment tag id</returns>
        private string ParseEnvironmentName(string name)
        {
            return (!String.IsNullOrEmpty(name)) ? "" 
                : (name == "Development") ? "Dev" 
                : (name == "Staging") ? "UAT" 
                : (name == "Production") ? "Prod" 
                : name;
        }

        /// <summary>
        /// Call local Consul DNS to get the base address of the service
        /// </summary>
        /// <returns></returns>
        private async Task<string> LookupBaseAddress()
        {
            if (_cancellation_token.IsCancellationRequested)
            {
                _logger.LogInformation("Stopping Process ...");
                return null;
            }
            _logger.LogTrace("Start DNS Lookup");
            var result = await _dns.ResolveServiceAsync(BASE_DOMAIN, SERVICE_NAME, _env );
            var host = result.First();
            var address = host.AddressList?.FirstOrDefault();
            var port = host.Port;
            var baseaddress = address?.ToString() ?? host.HostName;
            
            return $"{baseaddress}:{ port}";
        }

        /// <summary>
        /// Process entry point
        /// </summary>
        /// <param name="token">Cancelation token</param>
        /// <returns></returns>
        public async Task Run(CancellationToken token)
        {
            _cancellation_token = token;

            try
            {
                string baseAddress = await noDnsPolicy.ExecuteAsync( () => LookupBaseAddress() );

                if (!String.IsNullOrEmpty(baseAddress))
                {
                   await httpRequestPolicy.ExecuteAsync( async () =>
                   {
                       using (var client = new HttpClient())
                       {
                           var serviceResult = await client.GetStringAsync($"http://{baseAddress}/{_options.Value.EndPoint}/");
                           Console.WriteLine(serviceResult);
                       }
                   });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw e;
            }
            finally
            {
                Thread.Sleep(3000);
            }
        }
    }
}
