﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogle.Repository.Sql;

namespace Ogle.Repository.MySql
{
	public static class ServiceCollectionExtensions
	{
        private static IServiceCollection AddOgleMySqlRepository<TMetrics>(this IServiceCollection services)
        {
            services.AddTransient<ILogMetricsRepository<TMetrics>, OgleMySqlRepository<TMetrics>>();

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

