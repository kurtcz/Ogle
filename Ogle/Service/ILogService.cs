using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ogle
{
	internal interface ILogService<TGroupKey, TRecord, TMetrics>
		where TGroupKey: new()
		where TRecord: TGroupKey, new()
		where TMetrics: TGroupKey, new()
	{
		Task<IEnumerable<TRecord>> GetLogRecords(LogReaderOptions options);
        Task<IEnumerable<TMetrics>> GetLogMetrics(LogReaderOptions options, Func<IEnumerable<IGrouping<TGroupKey, TRecord>>, object> groupFunction);
		Task<bool> HasLogMetrics(DateOnly date);
		Task<bool> DeleteLogMetrics(DateOnly date);
		Task<long> SaveLogMetrics(DateOnly date, IEnumerable<TMetrics> metrics);
		Task<string> GetLogContent(string searchId, DateOnly date);
		IEnumerable<string> GetLogFilenames(DateOnly? date);
		Stream GetFileStreamWithoutLocking(string filename);
    }
}

