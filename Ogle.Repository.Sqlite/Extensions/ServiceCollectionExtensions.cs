using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogle.Repository.Sql;

namespace Ogle.Repository.Sqlite
{
	public static class ServiceCollectionExtensions
	{
		private static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services)
		{
			services.AddTransient<ILogMetricsRepository<TMetrics>, OgleSqliteRepository<TMetrics>>();

			return services;
		}

        public static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgleSqliteRepository<TMetrics>();
			services.Configure<OgleSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleSqliteRepository<TMetrics>(this IServiceCollection services, Action<OgleSqlRepositoryOptions> configurationAction)
        {
            services.AddOgleSqliteRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}