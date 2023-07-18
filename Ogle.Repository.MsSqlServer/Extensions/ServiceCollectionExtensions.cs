﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ogle.Repository.MsSqlServer
{
	public static class ServiceCollectionExtensions
	{
		private static IServiceCollection AddOgleSQLiteRepository<TMetrics>(this IServiceCollection services)
		{
			services.AddTransient<ILogMetricsRepository<TMetrics>, OgleSQLiteRepository<TMetrics>>();

			return services;
		}

        public static IServiceCollection AddOgleSQLiteRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgleSQLiteRepository<TMetrics>();
			services.Configure<OgleMsSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleSQLiteRepository<TMetrics>(this IServiceCollection services, Action<OgleMsSqlRepositoryOptions> configurationAction)
        {
            services.AddOgleSQLiteRepository<TMetrics>();
            services.Configure<OgleMsSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}