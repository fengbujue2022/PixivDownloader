using HangfireServer.Code;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PixivApi.Net;
using PixivApi.Net.API;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireServer.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            var serviceTypes = typeof(Startup).Assembly.GetTypes().Where(t => typeof(Services.IService).IsAssignableFrom(t) && !t.IsInterface);
            foreach (var t in serviceTypes)
            {
                services.AddScoped(t);
            }
            return services;
        }

        public static IServiceCollection AddPixivApi(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton((provider) =>
            {
                return new PixivApiClientFactory(
                    configuration.GetValue<string>("PUsername"),
                    configuration.GetValue<string>("Password"),
                    config =>
                    {
                        config.HttpClientSettings.ActionFilters.Add(new RateLimitFilterAttribute(provider.GetService<TimeLimiter>()));
                    });
            });
            services.AddScoped((provider) =>
            {
                return provider.GetService<PixivApiClientFactory>().Create<IPixivApiClient>();
            });
            return services;
        }

        public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(x => TimeLimiter.GetFromMaxCountByInterval(configuration.GetValue<int>("RateLimitPerCount"), TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimitPerSecond"))));
            return services;
        }
    }
}
