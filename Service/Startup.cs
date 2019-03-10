using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using TestService.Configuration;
using TestService.Helpers;

namespace TestService
{
    public class Startup
    {
        private readonly CancellationTokenSource _consulCancellationSource = new CancellationTokenSource();
        private IServiceCollection _services;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<ServiceDiscovery>(Configuration.GetSection("ServiceDiscovery"));
            services.Configure<DemoAppSettings>(Configuration.GetSection("DemoAppSettings"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHealthChecks();
            services.AddDiscoveryService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHealthChecks("/hc");
            app.RegisterDiscoveryService();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
