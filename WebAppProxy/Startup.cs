using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using WebAppProxy.Config;
using WebAppProxy.Services;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.Abstractions.Config;
using Yarp.ReverseProxy.Middleware;
using Yarp.ReverseProxy.RuntimeModel;

namespace WebAppProxy
{
    public class Startup
    {
        private const string DEBUG_HEADER = "Debug";
        private const string DEBUG_METADATA_KEY = "debug";
        private const string DEBUG_VALUE = "true";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the reverse proxy to capability to the server
            //var proxyBuilder = services.AddReverseProxy();
            // Initialize the reverse proxy from the "ReverseProxy" section of configuration
            //proxyBuilder.LoadFromConfig(Configuration.GetSection("ReverseProxy"));
            services.AddSingleton<ILoadConfigService, LoadConfigService>();
            services.AddReverseProxy().Load(GetRoutes(), GetClusters())
                //.AddTransforms(builderContext =>
                //{
                //builderContext.AddPathPrefix("/aigitalspace");
                //builderContext.AddPathRemovePrefix("/aigitalspace");//delete 'aigitalspace' prefix, is run ok
                //});
                .AddTransforms<MyTransformProvider>();
                //.AddTransformFactory<>();
            services.AddControllers();
            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAppProxy", Version = "v1" });
            //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseSwagger();
                //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppProxy v1"));
            }

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapReverseProxy();
                endpoints.MapReverseProxy(proxyPipeline =>
                {
                    // Use a custom proxy middleware, defined below
                    proxyPipeline.Use(MyCustomProxyStep);
                    // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
                    proxyPipeline.UseSessionAffinity();
                    proxyPipeline.UseLoadBalancing();
                });
            });
        }

        private ProxyRoute[] GetRoutes()
        {
            var proxyRoute = new ProxyRoute()
            {
                RouteId = "route1",
                ClusterId = "cluster1",
                Match = new RouteMatch
                {
                    // Path or Hosts are required for each route. This catch-all pattern matches all request paths.
                    //Path = "{**catch-all}"
                    //Path = "aigitalspace/api/{controller}/{action}"
                    Path = "aigitalspace/{**catch-all}"
                },
                Transforms = new List<Dictionary<string, string>> {
                    new Dictionary<string, string>
                    {
                        { "PathRemovePrefix","/aigitalspace"}
                    }
                }
            };
            return new[]
            {
                proxyRoute
            };
        }
        private Cluster[] GetClusters()
        {
            var debugMetadata = new Dictionary<string, string>();
            debugMetadata.Add(DEBUG_METADATA_KEY, DEBUG_VALUE);

            return new[]
            {
                new Cluster()
                {
                    Id = "cluster1",
                    HealthCheck=new HealthCheckOptions{
                        Active=new ActiveHealthCheckOptions{
                            Enabled=true,
                            Interval=TimeSpan.FromSeconds(2),
                            Timeout=TimeSpan.FromSeconds(5),
                            Policy="ConsecutiveFailures",
                            Path="/weatherforecast"
                        }
                    },
                    //LoadBalancingPolicy="Random",
                    //SessionAffinity = new SessionAffinityOptions { Enabled = true, Mode = "Cookie" },
                    Destinations = new Dictionary<string, Destination>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "destination1", new Destination{ Address="http://10.0.102.176:5006/" }},
                        { "destination2", new Destination{ Address="http://localhost:5000/"}}
                        //{ "debugdestination1", new Destination{ Address="https://bing.com",Metadata=debugMetadata}},
                    }
                }
            };
        }
        /// <summary>
        /// Custom proxy step that filters destinations based on a header in the inbound request
        /// Looks at each destination metadata, and filters in/out based on their debug flag and the inbound header
        /// </summary>
        public Task MyCustomProxyStep(HttpContext context, Func<Task> next)
        {
            // Can read data from the request via the context
            var useDebugDestinations = context.Request.Headers.TryGetValue(DEBUG_HEADER, out var headerValues) && headerValues.Count == 1 && headerValues[0] == DEBUG_VALUE;

            // The context also stores a ReverseProxyFeature which holds proxy specific data such as the cluster, route and destinations
            var availableDestinationsFeature = context.Features.Get<IReverseProxyFeature>();
            var filteredDestinations = new List<DestinationInfo>();

            // Filter destinations based on criteria
            foreach (var d in availableDestinationsFeature.AvailableDestinations)
            {
                //Todo: Replace with a lookup of metadata - but not currently exposed correctly here
                if (d.DestinationId.Contains("debug") == useDebugDestinations) { filteredDestinations.Add(d); }
            }
            availableDestinationsFeature.AvailableDestinations = filteredDestinations;

            // Important - required to move to the next step in the proxy pipeline
            return next();
        }
    }
}
