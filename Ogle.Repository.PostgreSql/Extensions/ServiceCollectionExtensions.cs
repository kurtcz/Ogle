using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ogle.Repository.Sql.Abstractions;

namespace Ogle.Repository.PostgreSql
{
	public static class ServiceCollectionExtensions
	{
        private static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services)
        {
            services.AddTransient<ILogMetricsRepository<TMetrics>, OglePostgreSqlRepository<TMetrics>>();

            return services;
        }

        public static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgleMySqlRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services, Action<OgleSqlRepositoryOptions> configurationAction)
        {
            services.AddOgleMySqlRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}

