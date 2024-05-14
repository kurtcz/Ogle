using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ogle.Repository.Sql.Abstractions;

namespace Ogle.Repository.PostgreSql
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services)
            where TMetrics : new()
        {
            services.AddTransient<ILogMetricsRepository<TMetrics>, OglePostgreSqlRepository<TMetrics>>();

            return services;
        }

        public static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
            where TMetrics : new()
        {
            services.AddOgleMySqlRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services, Action<OgleSqlRepositoryOptions> configurationAction)
            where TMetrics : new()
        {
            services.AddOgleMySqlRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}

