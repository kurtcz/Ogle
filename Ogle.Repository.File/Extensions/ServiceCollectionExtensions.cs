using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ogle.Repository.File
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddOgleFileRepository<TMetrics>(this IServiceCollection services)
        {
            services.AddTransient<ILogMetricsRepository<TMetrics>, OgleFileRepository<TMetrics>>();

            return services;
        }

        public static IServiceCollection AddOgleFileRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgleFileRepository<TMetrics>();
            services.Configure<OgleFileRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleFileRepository<TMetrics>(this IServiceCollection services, Action<OgleFileRepositoryOptions> configurationAction)
        {
            services.AddOgleFileRepository<TMetrics>();
            services.Configure<OgleFileRepositoryOptions>(configurationAction);

            return services;
        }
    }
}

