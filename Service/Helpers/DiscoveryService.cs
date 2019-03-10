using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestService.Configuration;
using Winton.Extensions.Configuration.Consul;

namespace TestService.Helpers
{
    public static class DiscoveryService
    {
        public static void AddDiscoveryService(this IServiceCollection services)
        {
            services.AddSingleton<IConsulClient>(p => new ConsulClient(cfg =>
            {
                var serviceConfiguration = p.GetRequiredService<IOptions<ServiceDiscovery>>().Value;

                if (!string.IsNullOrEmpty(serviceConfiguration.Consul.HttpEndpoint.ToUri().AbsoluteUri))
                {
                    cfg.Address = new Uri(serviceConfiguration.Consul.HttpEndpoint.ToUri().AbsoluteUri);
                }
            }));

            services.AddSingleton<IConsulRegistrationHelper, ConsulRegistrationHelper>();
        }


        public static void RegisterDiscoveryService(this IApplicationBuilder app)
        {
            var features = app.Properties["server.Features"] as FeatureCollection;
            var consulHelper = app.ApplicationServices.GetService<IConsulRegistrationHelper>();
            var addresses = features.Get<IServerAddressesFeature>()
                .Addresses
                .Select(p => new Uri(p));

            foreach (var url in addresses)
                consulHelper.AddService(url);

            consulHelper.Register();
        }

        public static IConfigurationBuilder Config(this IHostingEnvironment env, CancellationTokenSource consulCancellationSource, ServiceDiscovery serviceConfiguration)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(env.ContentRootPath)
           .AddConsul(
               $"{env.ApplicationName}/{env.EnvironmentAttribute()}/appsettings.json",
               consulCancellationSource.Token, options =>
                {
                    options.ConsulConfigurationOptions =
                        cco => { cco.Address = serviceConfiguration.Consul.HttpEndpoint.ToUri(); };
                    options.Optional = false;
                    options.ReloadOnChange = true;
                    options.OnLoadException = exceptionContext => 
                    {
                        exceptionContext.Ignore = true;
                    };
                });

            return builder;
        }

        public static string EnvironmentAttribute(this IHostingEnvironment env)
        {
            return (env.IsDevelopment()) ? "Dev"
               : (env.IsStaging()) ? "UAT"
               : (env.IsProduction()) ? "Prod"
               : env.EnvironmentName;
        }
    }
}
