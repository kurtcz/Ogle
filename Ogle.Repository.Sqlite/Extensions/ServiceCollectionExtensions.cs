using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ogle.Repository.Sql.Abstractions;

namespace Ogle.Repository.Sqlite
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services)
            where TMetrics : new()
        {
            services.AddTransient<ILogMetricsRepository<TMetrics>, OgleSqliteRepository<TMetrics>>();

            return services;
        }

        public static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
            where TMetrics : new()
        {
            services.AddOgleSqliteRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services, Action<OgleSqlRepositoryOptions> configurationAction)
            where TMetrics : new()
        {
            services.AddOgleSqliteRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}