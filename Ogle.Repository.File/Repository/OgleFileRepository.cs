using Microsoft.Extensions.Options;
using Ogle.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ogle.Repository.File
{
    public class OgleFileRepository<TMetrics> : ILogMetricsRepository<TMetrics>
    {
        const string SearchPattern = @"OgleMetrics-*.json";
        const string DetailedSearchPattern = @"OgleMetricsDetails-*.json";
        readonly string FileMask;
        readonly string DetailedFileMask;

        protected IOptionsMonitor<OgleFileRepositoryOptions> Settings { get; }

        public OgleFileRepository(IOptionsMonitor<OgleFileRepositoryOptions> settings)
        {
            Settings = settings;
            FileMask = SearchPattern.Replace("*", "{0:yyyy-MM-dd}");
            DetailedFileMask = DetailedSearchPattern.Replace("*", "{0:yyyy-MM-dd}");
        }

        #region ILogMetricsRepository methods

        public virtual async Task<bool> HasMetrics(DateTime from, DateTime to, bool detailedGroupping)
        {
            if (!Directory.Exists(Settings.CurrentValue.Folder))
            {
                Directory.CreateDirectory(Settings.CurrentValue.Folder);
            }
            var searchPattern = detailedGroupping ? DetailedSearchPattern : SearchPattern;
            var files = Directory.GetFiles(Settings.CurrentValue.Folder, searchPattern);

            for (var date = from; date < to; date = date.AddMinutes(1))
            {
                var fileMask = detailedGroupping ? DetailedFileMask : FileMask;
                var expectedFile = string.Format(fileMask, date);

                if (files.Select(i => Path.GetFileName(i)).Any(i => i == expectedFile))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual async Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to, bool detailedGroupping)
        {
            var metrics = new List<TMetrics>();
            var props = typeof(TMetrics).GetProperties();
            var timeBucketProp = props.Single(i => i.GetCustomAttributes(true).Any(j => j is TimeBucketAttribute));

            if (!Directory.Exists(Settings.CurrentValue.Folder))
            {
                Directory.CreateDirectory(Settings.CurrentValue.Folder);
            }

            foreach (var path in GetFilesFromInterval(from, to, detailedGroupping))
            {
                var content = await System.IO.File.ReadAllTextAsync(path);
                var batch = JsonSerializer.Deserialize<IEnumerable<TMetrics>>(content);

                metrics.AddRange(batch);
            }

            var filtered = metrics.Where(i =>
            {
                var t = (DateTime)timeBucketProp.GetValue(i);

                return from <= t && t < to;
            }).ToList();

            return filtered;
        }

        public virtual async Task<bool> DeleteMetrics(DateTime from, DateTime to, bool detailedGroupping)
        {
            var filesDeleted = 0;

            if (!Directory.Exists(Settings.CurrentValue.Folder))
            {
                Directory.CreateDirectory(Settings.CurrentValue.Folder);
            }

            foreach (var path in GetFilesFromInterval(from, to, detailedGroupping))
            {
                System.IO.File.Delete(path);
                filesDeleted++;
            }

            return filesDeleted > 0;
        }

        public virtual async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics, bool detailedGroupping)
        {
            var props = typeof(TMetrics).GetProperties();
            var timeBucketProp = props.Single(i => i.GetCustomAttributes(true).Any(j => j is TimeBucketAttribute));
            var dict = new Dictionary<DateOnly, List<TMetrics>>();

            if (!Directory.Exists(Settings.CurrentValue.Folder))
            {
                Directory.CreateDirectory(Settings.CurrentValue.Folder);
            }

            foreach (var item in metrics)
            {
                var timeBucket = DateOnly.FromDateTime((DateTime)timeBucketProp.GetValue(item));

                if (!dict.ContainsKey(timeBucket))
                {
                    dict.Add(timeBucket, new List<TMetrics>());
                }
                dict[timeBucket].Add(item);
            }

            foreach(var date in dict.Keys)
            {
                var content = JsonSerializer.Serialize(dict[date]);
                var filename = string.Format(detailedGroupping? DetailedFileMask : FileMask, date);
                var path = Path.Combine(Settings.CurrentValue.Folder, filename);

                await System.IO.File.AppendAllTextAsync(path, content);
            }

            return metrics.Count();
        }

        public Dictionary<string, string> GetConfiguration()
        {
            return Settings.CurrentValue.ToPropertyDictionary();
        }

        #endregion

        #region Private methods

        private IEnumerable<string> GetFilesFromInterval(DateTime from, DateTime to, bool detailedGroupping)
        {
            var fileMask = detailedGroupping ? DetailedFileMask : FileMask;
            var fromFile = string.Format(fileMask, from);
            var toFile = string.Format(fileMask, to);

            return Directory.GetFiles(Settings.CurrentValue.Folder, SearchPattern, SearchOption.TopDirectoryOnly)
                            .Where(path => string.Compare(fromFile, Path.GetFileName(path)) <= 0 &&
                                           string.Compare(Path.GetFileName(path), toFile) < 0);
        }

        #endregion
    }
}

