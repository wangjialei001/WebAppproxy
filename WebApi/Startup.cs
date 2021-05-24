using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "WebApi",
                    Version = "v1",
                    Description = "测试平台",
                    Contact = new OpenApiContact
                    {
                        Name = "",
                        Email = "",
                        Url = null//new Uri("http://locahost:5000/apitest1")
                    }
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1"));
#else
            //string baseAppRoute = "apptest";//项目名称
            app.UseSwagger(c =>
            {
                //c.RouteTemplate = baseAppRoute + "/swagger/{documentName}/swagger.json";
                //c.RouteTemplate = "/swagger/{documentName}/swagger.json";
                //c.PreSerializeFilters.Add((swaggerDoc, httReq) =>
                //{
                //    IDictionary<string, OpenApiPathItem> paths = new Dictionary<string, OpenApiPathItem>();
                //    foreach (var path in swaggerDoc.Paths)
                //    {
                //        paths.TryAdd($"/{baseAppRoute}" + path.Key, path.Value);//修改swagger请求url地址
                //    }
                //    swaggerDoc.Paths.Clear();
                //    foreach (var path in paths)
                //    {
                //        swaggerDoc.Paths[path.Key] = path.Value;
                //    }
                //});
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    //根据访问地址，设置swagger服务路径
                    var proxyId = httpReq.Headers["X-Forwarded-For"];
                    var proxyScheme = httpReq.Headers["X-Forwarded-Proto"];
                    if (!string.IsNullOrEmpty(proxyScheme) && !string.IsNullOrEmpty(proxyId))
                    {
                        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{proxyScheme}://{proxyId}/{httpReq.Headers["X-Forwarded-Prefix"]}" } };
                    }
                    else
                    {
                        swagger.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/{httpReq.Headers["X-Forwarded-Prefix"]}" } };
                    }
                    Console.WriteLine($"{httpReq.Scheme}://{httpReq.Host.Value}/{httpReq.Headers["X-Forwarded-Prefix"]}");
                });
            });
            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint($"/{baseAppRoute}/swagger/v1/swagger.json", "WebApi");//修改页面显示请求地址
                //c.SwaggerEndpoint($"/swagger/v1/swagger.json", "WeiApi");
                //使用相对路径
                c.SwaggerEndpoint($"v1/swagger.json", "WeiApi");
                //c.SwaggerEndpoint($"/{baseAppRoute}/swagger/v1.0/swagger.json", "WeiCloud.DigitalSpaceApi");
                //c.RoutePrefix = $"{baseAppRoute}";
                c.OAuthClientId("WebApi");
            });
#endif

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}