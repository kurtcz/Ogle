using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ogle
{
	public static class ServiceCollectionExtensions
	{
		private static IServiceCollection AddOgle(this IServiceCollection services)
		{
			services.AddOptions<OgleOptions>()
					.Configure(options =>
					{
						options.DefaultMinutesPerBucket = 60;
						options.DefaultNumberOfBuckets = 24;
						options.DrillDownMinutesPerBucket = 1;
						options.DrillDownNumberOfBuckets = 60;
						options.LogParserTimeout = TimeSpan.FromSeconds(300);
						options.AllowedSearchPattern = @"\S+";
						options.LogReaderBackBufferCapacity = 64;
						options.DatasetColors = new[]
						{
							"#cf2233",
							"#f26946",
							"#fae80b",
							"#62c742",
							"#2d63af",
							"#4d3292",
							"#f9cde0",
							"#81592f"
						};
					});

			return services;
		}

        private static IServiceCollection AddOgle(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.AddOgle();

			//If DatasetColors are defined in appsetings.json we need to clear the default array
			//as the new array will be appended to the default one
			if (configurationSection.GetValue<string[]>("DatasetColors")?.Any() ?? false)
			{
				services.Configure<OgleOptions>(options =>
				{
					options.DatasetColors = new string[0];
				});
			}
            services.Configure<OgleOptions>(configurationSection);

            return services;
        }

        public static IServiceCollection AddOgle(this IServiceCollection services, Action<OgleOptions> configurationAction)
		{
			services.AddOgle();
			services.Configure(configurationAction);

			return services;
		}

        public static IServiceCollection AddOgle(this IServiceCollection services, IConfiguration configurationSection, Action<OgleOptions> configurationAction)
		{
			services.AddOgle(configurationSection);
			services.Configure(configurationAction);

			return services;
		}
    }
}

