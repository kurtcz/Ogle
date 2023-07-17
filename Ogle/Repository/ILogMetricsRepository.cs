namespace Ogle
{
	public interface ILogMetricsRepository<TMetrics>
    {
		Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to);
		Task<long> SaveMetrics(IEnumerable<TMetrics> metrics);
	}
}

