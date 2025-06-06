using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ogle
{
    public interface ILogMetricsRepository<TMetrics>
    {
        Task<bool> DeleteMetrics(DateTime from, DateTime to, bool detailedGroupping);
        Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to, bool detailedGroupping);
        Task<bool> HasMetrics(DateTime from, DateTime to, bool detailedGroupping);
        Task<long> SaveMetrics(IEnumerable<TMetrics> metrics, bool detailedGroupping);
        Dictionary<string, string> GetConfiguration();
    }
}

