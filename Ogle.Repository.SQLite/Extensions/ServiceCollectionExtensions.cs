using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogle.Repository.SQLite;

namespace Ogle.Repository.SQLite
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
			services.Configure<OgleSQLiteRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleSQLiteRepository<TMetrics>(this IServiceCollection services, Action<OgleSQLiteRepositoryOptions> configurationAction)
        {
            services.AddOgleSQLiteRepository<TMetrics>();
            services.Configure<OgleSQLiteRepositoryOptions>(configurationAction);

            return services;
        }
    }
}