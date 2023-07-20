using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ogle
{
	public interface ILogMetricsRepository<TMetrics>
    {
        Task<bool> DeleteMetrics(DateTime from, DateTime to);
        Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to);
		Task<bool> HasMetrics(DateTime from, DateTime to);
		Task<long> SaveMetrics(IEnumerable<TMetrics> metrics);
	}
}

