using Consul;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestService.Configuration;

namespace TestService.Helpers
{
    public class ConsulRegistrationHelper : IConsulRegistrationHelper
    {
        private List<AgentServiceRegistration> _registrations = new List<AgentServiceRegistration>();

        private readonly ServiceDiscovery _options;
        private readonly string _name;
        private readonly string _env;
        private readonly ILogger _logger;
        private readonly IConsulClient _client;
        private readonly IApplicationLifetime _applife;
        public ConsulRegistrationHelper(IOptions<ServiceDiscovery> serviceOptions,
            IConsulClient client,
            IApplicationLifetime appLife, 
            IHostingEnvironment env,
            ILogger<ConsulRegistrationHelper> logger)
        {
            _options = serviceOptions.Value;
            _name = env.ApplicationName;
            _applife = appLife;
            _logger = logger;
            _client = client;
            _env = env.EnvironmentAttribute();
        }

        public void AddService(Uri address)
        {
            var sId = $"{_name}_{address.Host}:{address.Port}";

            var httpCheck = new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                Interval = TimeSpan.FromSeconds(30),
                HTTP = new Uri(address, "hc").OriginalString
            };

            _registrations.Add( new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                Address = address.Host,
                ID = sId,
                Name = _name,
                Tags = new string[] { _env },
                Port = address.Port
            });
        }

        public void Register()
        {
            _applife.ApplicationStarted.Register( () => {
                foreach (var registrations in _registrations)
                    _client.Agent.ServiceRegister(registrations);
             });

            _applife.ApplicationStopping.Register( () => { 
                foreach (var registrations in _registrations)
                    _client.Agent.ServiceDeregister(registrations.ID);
            });
        }
    }
}
