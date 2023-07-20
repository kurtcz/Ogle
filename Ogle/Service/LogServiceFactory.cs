using System;
using Microsoft.Extensions.Options;

namespace Ogle
{
	internal static class LogServiceFactory
	{
		public static dynamic CreateInstance(IOptionsMonitor<OgleOptions> settings, object? repo = null)
		{
            var logServiceType = typeof(LogService<,,>).MakeGenericType(new[]
            {
                    settings.CurrentValue.GroupKeyType,
                    settings.CurrentValue.RecordType,
                    settings.CurrentValue.MetricsType
                });
            dynamic logService = Activator.CreateInstance(logServiceType, new[] { settings, repo });

            return logService;
        }


    }
}

