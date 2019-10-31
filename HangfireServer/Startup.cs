﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PixivApi.Net;
using PixivApi.Net.API;

namespace HangfireServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            //hangfire
            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
            });
            //pixiv api
            services.AddSingleton<PixivApiClientFactory>((provider) =>
            {
                return new PixivApiClientFactory(
                    Configuration.GetValue<string>("PUsername"),
                    Configuration.GetValue<string>("Password"));
            });
            services.AddTransient<IPixivApiClient>((provider) =>
            {
                return provider.GetService<PixivApiClientFactory>().Create<IPixivApiClient>();
            });
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHangfireDashboard();
            app.UseHangfireServer(new BackgroundJobServerOptions()
            {
                WorkerCount = Configuration.GetValue<int>("WorkerCount")
            });
            app.UseMvc();
        }
    }
}