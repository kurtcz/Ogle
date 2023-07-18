using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogle.Repository.Sql;

namespace Ogle.Repository.MsSqlServer
{
	public static class ServiceCollectionExtensions
	{
		private static IServiceCollection AddOgleMsSqlServerRepository<TMetrics>(this IServiceCollection services)
		{
			services.AddTransient<ILogMetricsRepository<TMetrics>, OgleMsSqlServerRepository<TMetrics>>();

			return services;
		}

        public static IServiceCollection AddOgleMsSqlServerRepository<TMetrics>(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgleMsSqlServerRepository<TMetrics>();
			services.Configure<OgleSqlRepositoryOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgleMsSqlServerRepository<TMetrics>(this IServiceCollection services, Action<OgleSqlRepositoryOptions> configurationAction)
        {
            services.AddOgleMsSqlServerRepository<TMetrics>();
            services.Configure<OgleSqlRepositoryOptions>(configurationAction);

            return services;
        }
    }
}