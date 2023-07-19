using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Ogle
{
	internal class LogService<TGroupKey, TRecord, TMetrics> : ILogService<TGroupKey, TRecord, TMetrics>
        where TGroupKey : new()
        where TRecord : TGroupKey, new()
        where TMetrics : TGroupKey, new()
    {
        private readonly IOptionsMonitor<OgleOptions> _settings;
        private readonly ILogMetricsRepository<TMetrics> _repo;

        public LogService(IOptionsMonitor<OgleOptions> settings) : this(settings, null)
		{
		}

        public LogService(IOptionsMonitor<OgleOptions> settings, ILogMetricsRepository<TMetrics> repo)
        {
            _settings = settings;
            _repo = repo;
        }

        public async Task<IEnumerable<TRecord>> GetLogRecords(LogReaderOptions options)
        {
            var dict = new Dictionary<string, TRecord>();
            var props = typeof(TRecord).GetProperties();
            var mandatoryAttribute = typeof(TRecord).GetCustomAttributes(true)
                                                    .Single(i => i is MandatoryLogPatternAttribute) as MandatoryLogPatternAttribute;
            var mandatoryProps = props.Where(i => i.GetCustomAttributes(true).Any(j => j is MandatoryAttribute))
                                      .ToArray();
            var mandatoryPatterns = mandatoryProps.ToDictionary(k => k,
                                                                v => v.GetCustomAttributes(true)
                                                                      .Single(j => j is MandatoryAttribute) as MandatoryAttribute);
            var timeBucketProp = props.Single(i => i.GetCustomAttributes(true).Any(j => j is TimeBucketAttribute));
            var keyAttribute = mandatoryProps.SelectMany(i => i.GetCustomAttributes(true)
                                                               .Where(j => j is MandatoryAttribute)
                                                               .Cast<MandatoryAttribute>())
                                             .Single(i => i.IsKey);
            var keyProp = props.Single(i => i.GetCustomAttributes(true)
                                             .Any(j => (j as MandatoryAttribute)?.IsKey ?? false));
            var patterns = props.Where(i => i.GetCustomAttributes(true)
                                             .Any(j => j is LogPatternAttribute))
                                .ToDictionary(k => k,
                                              v => v.GetCustomAttributes(true)
                                                    .Single(j => j is LogPatternAttribute) as LogPatternAttribute);
            var lineNumber = 0;

            foreach(var line in ReadLogs(options.Date.Value))
            {
                var mandatoryMatch = mandatoryAttribute.Regex.Match(line);

                lineNumber++;
                if(!mandatoryMatch.Success)
                {
                    continue;
                }

                TRecord record;

                var id = mandatoryMatch.Groups[keyAttribute.MatchGroup].Value;

                if (!dict.ContainsKey(id))
                {
                    record = new TRecord();
                    keyProp.SetValue(record, id);
                    dict.Add(id, record);

                    foreach(var patternInfo in mandatoryPatterns.Where(i => i.Key.Name != keyProp.Name))
                    {
                        //var typeCode = Type.GetTypeCode(patternInfo.Key.PropertyType);
                        var value = ParseValue(patternInfo.Key.PropertyType, mandatoryMatch.Groups[patternInfo.Value.MatchGroup].Value, options.Date.Value, patternInfo.Value.Format);

                        if (timeBucketProp.Name == patternInfo.Key.Name)
                        {
                            var timeBucket = DateTimeToBucket((DateTime)value, options);

                            value = new DateTime(options.Date.Value.Year,
                                                 options.Date.Value.Month,
                                                 options.Date.Value.Day,
                                                 timeBucket.Hour,
                                                 timeBucket.Minute,
                                                 timeBucket.Second,
                                                 timeBucket.Millisecond);
                        }

                        patternInfo.Key.SetValue(record, value);
                    }
                }

                record = dict[id];

                foreach(var patternInfo in patterns)
                {
                    var oldValue = patternInfo.Key.GetValue(record);
                    object? defaultValue = GetDefaultValueForType(patternInfo.Key.PropertyType);

                    if (oldValue != null && !oldValue.Equals(defaultValue))
                    {
                        continue;
                    }

                    //string.Contains() is much faster than Regex.Match()
                    //let's use it to quickly filter out lines we are not interested in
                    if(patternInfo.Value.Filter != null && !line.Contains(patternInfo.Value.Filter))
                    {
                        continue;
                    }

                    var match = patternInfo.Value.Regex.Match(line);

                    if (!match.Success)
                    {
                        continue;
                    }

                    object? value = ParseValue(patternInfo.Key.PropertyType, match.Groups[patternInfo.Value.MatchGroup].Value, options.Date.Value, patternInfo.Value.Format);

                    if (oldValue != value)
                    {
                        patternInfo.Key.SetValue(record, value);
                    }
                }
            }

            var minTime = new DateTime(options.Date.Value.Year, options.Date.Value.Month, options.Date.Value.Day, options.HourFrom, options.MinuteFrom, 0);
            var maxTime = minTime.AddMinutes(options.MinutesPerBucket.Value * options.NumberOfBuckets.Value);
            var filtered = dict.Values.Where(i =>
            {
                var t = (DateTime)timeBucketProp.GetValue(i);

                return minTime <= t && t < maxTime;
            }).ToList();

            return filtered;
        }

        public async Task<IEnumerable<TMetrics>> GetLogMetrics(LogReaderOptions options, Func<IEnumerable<IGrouping<TGroupKey, TRecord>>, object> groupFunction)
        {
            var result = Enumerable.Empty<TMetrics>();

            if (_repo != null)
            {
                var from = new DateTime(options.Date.Value.Year,
                                        options.Date.Value.Month,
                                        options.Date.Value.Day,
                                        options.HourFrom,
                                        options.MinuteFrom,
                                        0);
                var to = from.AddMinutes(options.MinutesPerBucket.Value * options.NumberOfBuckets.Value);

                result = await _repo.GetMetrics(from, to);
            }

            if (!result.Any())
            {
                var metrics = await GetLogRecords(options);
                var dict = new Dictionary<TGroupKey, List<TRecord>>();
                var groupped = metrics.GroupBy(i => (TGroupKey)i);

                result = (IEnumerable<TMetrics>)groupFunction(groupped);
            }

            return result;
        }

        public async Task<long> SaveLogMetrics(DateOnly date, IEnumerable<TMetrics> metrics)
        {
            return await _repo.SaveMetrics(metrics.Cast<TMetrics>());
        }

        public async Task<string> GetLogContent(string searchTerm, DateOnly date)
        {
            var props = typeof(TRecord).GetProperties();
            var mandatoryAttribute = typeof(TRecord).GetCustomAttributes(true)
                                                    .Single(i => i is MandatoryLogPatternAttribute) as MandatoryLogPatternAttribute;
            var mandatoryProps = props.Where(i => i.GetCustomAttributes(true).Any(j => j is MandatoryAttribute))
                                      .ToArray();
            var mandatoryPatterns = mandatoryProps.ToDictionary(k => k,
                                                                v => v.GetCustomAttributes(true)
                                                                      .Single(j => j is MandatoryAttribute) as MandatoryAttribute);
            var keyAttribute = mandatoryProps.SelectMany(i => i.GetCustomAttributes(true)
                                                               .Where(j => j is MandatoryAttribute)
                                                               .Cast<MandatoryAttribute>())
                                             .Single(i => i.IsKey);
            var keyProp = props.Single(i => i.GetCustomAttributes(true)
                                             .Any(j => (j as MandatoryAttribute)?.IsKey ?? false));
            var keys = new HashSet<string>();
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(keyAttribute.SearchPattern))
            {
                throw new InvalidOperationException("MandatoryAttribute that has IsKey property set must also define a SearchPattern property");
            }

            foreach(var logLine in ReadLogs(date))
            {
                if (logLine.Contains(searchTerm))
                {
                    var match = mandatoryAttribute.Regex.Match(logLine);

                    keys.Add(match.Groups[keyAttribute.MatchGroup].Value);
                }
                if(keys.Any(key => logLine.Contains(key)))
                {
                    sb.AppendLine(logLine);
                }
            }

            return sb.ToString();
        }

        public IEnumerable<string> GetLogFilenames(DateOnly? date)
        {
            var logFiles = Directory.GetFiles(_settings.CurrentValue.LogFolder,
                                              string.Format(_settings.CurrentValue.LogFilePattern, date))
                                    .OrderBy(path => path);

            return logFiles;
        }

        public IEnumerable<string> ReadLogs(DateOnly date)
        {
            foreach(var file in GetLogFilenames(date))
            {
                var logLines = ReadLinesWithoutLocking(file);

                foreach(var line in logLines)
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<string> ReadLinesWithoutLocking(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using(var sr = new StreamReader(fs))
                {
                    while(!sr.EndOfStream)
                    {
                        yield return sr.ReadLine();
                    }
                }
            }
        }

        public Stream GetFileStreamWithoutLocking(string filename)
        {
            var path = Path.Combine(_settings.CurrentValue.LogFolder, filename);

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        private static DateTime DateTimeToBucket(DateTime timestamp, LogReaderOptions options)
        {
            var bucket = TimeSpan.FromMinutes(options.MinutesPerBucket.Value);
            var dt = new DateTime(timestamp.Ticks - (timestamp.Ticks % bucket.Ticks));

            return dt;
        }

        private static object? GetDefaultValueForType(Type type)
        {
            object? defaultValue = null;

            if (type == typeof(bool))
            {
                defaultValue = default(bool);
            }
            else if (type == typeof(short))
            {
                defaultValue = default(short);
            }
            else if (type == typeof(int))
            {
                defaultValue = default(int);
            }
            else if (type == typeof(long))
            {
                defaultValue = default(long);
            }
            else if (type == typeof(ushort))
            {
                defaultValue = default(ushort);
            }
            else if (type == typeof(uint))
            {
                defaultValue = default(uint);
            }
            else if (type == typeof(ulong))
            {
                defaultValue = default(ulong);
            }
            else if (type == typeof(float))
            {
                defaultValue = default(float);
            }
            else if (type == typeof(double))
            {
                defaultValue = default(double);
            }
            else if (type == typeof(decimal))
            {
                defaultValue = default(decimal);
            }
            else if (type == typeof(DateTime))
            {
                defaultValue = default(DateTime);
            }
            else if (type == typeof(TimeSpan))
            {
                defaultValue = default(TimeSpan);
            }
            else if (type == typeof(string))
            {
                defaultValue = default(string);
            }

            return defaultValue;
        }

        private static object? ParseValue(Type type, string stringValue, DateOnly date, string format)
        {
            object? value = null;

            if (type == typeof(bool))
            {
                value = stringValue != null;
            }
            else if (type == typeof(short))
            {
                value = short.Parse(stringValue);
            }
            else if (type == typeof(int))
            {
                value = int.Parse(stringValue);
            }
            else if (type == typeof(long))
            {
                value = long.Parse(stringValue);
            }
            else if (type == typeof(ushort))
            {
                value = ushort.Parse(stringValue);
            }
            else if (type == typeof(uint))
            {
                value = uint.Parse(stringValue);
            }
            else if (type == typeof(ulong))
            {
                value = ulong.Parse(stringValue);
            }
            else if (type == typeof(float))
            {
                value = float.Parse(stringValue);
            }
            else if (type == typeof(double))
            {
                value = double.Parse(stringValue);
            }
            else if (type == typeof(decimal))
            {
                value = decimal.Parse(stringValue);
            }
            else if (type == typeof(DateTime))
            {
                value = format == null ? DateTime.Parse(stringValue) : DateTime.ParseExact(stringValue, format, CultureInfo.InvariantCulture);
                var dt = value as DateTime? ?? default(DateTime);
                value = new DateTime(date.Year, date.Month, date.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
            }
            else if (type == typeof(TimeSpan))
            {
                value = format == null ? TimeSpan.Parse(stringValue) : TimeSpan.ParseExact(stringValue, format, CultureInfo.InvariantCulture);
            }
            else if (type == typeof(string))
            {
                value = stringValue;
            }

            return value;
        }

        public async Task<bool> HasLogMetrics(DateOnly date)
        {
            var from = new DateTime(date.Year, date.Month, date.Day);
            var to = from.AddDays(1);
            var b = await _repo.HasMetrics(from, to);

            return b;
        }

        public async Task<bool> DeleteLogMetrics(DateOnly date)
        {
            var from = new DateTime(date.Year, date.Month, date.Day);
            var to = from.AddDays(1);
            var b = await _repo.DeleteMetrics(from, to);

            return b;
        }
    }
}

