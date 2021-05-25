// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Reflection;

namespace IdentityServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            //const string connectionString = @"Data Source=(LocalDb)\MSSQLLocalDB;database=IdentityServer4.Quickstart.EntityFramework-4.0.0;trusted_connection=yes;";
            const string connectionString = @"server=127.0.0.1;uid=root;pwd=123456;port=3306;database=WeiCloudDB.SSO;default command timeout=10000;CharSet=utf8;SslMode=None";
            //const string connectionString = @"server=10.0.102.162;uid=root;pwd=Zrhdb#2019;port=3306;database=WeiCloudDB.SSO1;default command timeout=10000;CharSet=utf8;SslMode=None";

            var builder = services.AddIdentityServer()
                //用户依旧采用内存用户，可用Identity替换
                .AddTestUsers(TestUsers.Users)
                //添加配置数据（客户端、资源）
                .AddConfigurationStore(options =>
                {
                    //options.ConfigureDbContext = b => b.UseMySql(connectionString, ServerVersion.Parse("8.0.21"),
                    //    sql => sql.MigrationsAssembly(migrationsAssembly));
                    options.ConfigureDbContext = b => b.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                        sql =>
                        {
                            sql.MigrationsAssembly(migrationsAssembly);
                        });
                })
                //添加操作数据
                .AddOperationalStore(options =>
                {
                    //options.ConfigureDbContext = b => b.UseMySql(connectionString, ServerVersion.Parse("8.0.21"),
                    //    sql => sql.MigrationsAssembly(migrationsAssembly));
                    options.ConfigureDbContext = b => b.UseMySql(connectionString,ServerVersion.AutoDetect(connectionString),
                        sql => {
                            sql.MigrationsAssembly(migrationsAssembly);
                        });
                    options.EnableTokenCleanup = true;//token自动清理
                });

            builder.AddDeveloperSigningCredential();

            services.AddAuthentication()
                //.AddGoogle("Google", options =>
                //{
                //    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                //    options.ClientId = "<insert here>";
                //    options.ClientSecret = "<insert here>";
                //})
                .AddOpenIdConnect("oidc", "Demo IdentityServer", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.SaveTokens = true;

                    options.Authority = "https://demo.identityserver.io/";
                    options.ClientId = "interactive.confidential";
                    options.ClientSecret = "secret";
                    options.ResponseType = "code";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // this will do the initial DB population
            //InitializeDatabase(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var resource in Config.ApiScopes)
                    {
                        context.ApiScopes.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
