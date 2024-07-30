using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Index;

namespace Ogle
{
    internal interface ILogService<TGroupKey, TRecord, TMetrics>
        where TGroupKey : new()
        where TRecord : TGroupKey, new()
        where TMetrics : TGroupKey, new()
    {
        Task<IEnumerable<TRecord>> GetLogRecords(LogReaderOptions options);
        Task<IEnumerable<TMetrics>> GetLogMetrics(LogReaderOptions options, Func<IEnumerable<IGrouping<TGroupKey, TRecord>>, object> groupFunction);
        Task<bool> HasLogMetrics(DateOnly date, bool detailedGroupping);
        Task<bool> DeleteLogMetrics(DateOnly date, bool detailedGroupping);
        Task<long> SaveLogMetrics(DateOnly date, IEnumerable<TMetrics> metrics, bool detailedGroupping);
        Task<string> GetLogContent(string searchTerm, string? filePattern, DateOnly? date);
        string HighlightLogContent(string content, string searchTerm);
        IEnumerable<string> GetLogFilenames(DateOnly? date, string? filePattern);
        Stream GetFileStreamWithoutLocking(string filename);
        bool HasIndex(DateOnly date);
        void DeleteIndex(DateOnly date);
        IndexReader CreateIndex(DateOnly date, bool overwriteExisting);
    }
}

