﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Promitor.Core.Scraping;
using Promitor.Core.Scraping.Configuration.Providers;
using Promitor.Core.Scraping.Configuration.Providers.Interfaces;
using Promitor.Core.Telemetry;
using Promitor.Core.Telemetry.Interfaces;
using Promitor.Scraper.Host.Extensions;
using Promitor.Scraper.Host.Models;

namespace Promitor.Scraper.Host
{
    public class Startup
    {
        public Startup()
        {
            Configuration = BuildConfiguration();
            ScrapeEndpointBasePath = ScrapeEndpoint.GetBasePath(Configuration);
        }

        public IConfiguration Configuration { get; }
        public string ScrapeEndpointBasePath { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UsePrometheusScraper(ScrapeEndpointBasePath);
            app.UseOpenApiUi();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IMetricsDeclarationProvider, MetricsDeclarationProvider>();
            services.AddTransient<IExceptionTracker, ApplicationInsightsTelemetry>();
            services.AddTransient<ILogger, RuntimeLogger>();
            var healthMonitor = new HealthMonitor();
            services.AddSingleton<HealthMonitor>(healthMonitor);

            services.AddMvc()
                    .AddJsonOptions(jsonOptions =>
                    {
                        jsonOptions.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                        jsonOptions.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    });
            
            services.UseCronScheduler();
            services.UseOpenApiSpecifications(ScrapeEndpointBasePath, apiVersion: 1);
        }

        private IConfiguration BuildConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder.Build();
        }
    }
}