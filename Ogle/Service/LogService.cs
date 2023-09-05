using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Ogle
{
    internal class LogService<TGroupKey, TRecord, TMetrics> : ILogService<TGroupKey, TRecord, TMetrics>
        where TGroupKey : class, new()
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

            foreach (var line in ReadLogs(options.Date.Value))
            {
                var mandatoryMatch = mandatoryAttribute.Regex.Match(line);

                lineNumber++;
                if (!mandatoryMatch.Success)
                {
                    continue;
                }

                TRecord record;

                var id = mandatoryMatch.Groups[keyAttribute.MatchGroup].Value;

                //some events like application restarts do not have a request id
                //therefore we need to generate one on the fly to be able to aggregate them
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("D");
                }

                if (!dict.ContainsKey(id))
                {
                    record = new TRecord();
                    keyProp.SetValue(record, id);
                    dict.Add(id, record);

                    foreach (var patternInfo in mandatoryPatterns.Where(i => i.Key.Name != keyProp.Name))
                    {
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

                foreach (var patternInfo in patterns)
                {
                    var oldValue = patternInfo.Key.GetValue(record);
                    object? defaultValue = GetDefaultValueForType(patternInfo.Key.PropertyType);

                    if (oldValue != null && !oldValue.Equals(defaultValue))
                    {
                        continue;
                    }

                    //string.Contains() is much faster than Regex.Match()
                    //let's use it to quickly filter out lines we are not interested in
                    if (patternInfo.Value.Filter != null && !line.Contains(patternInfo.Value.Filter))
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
                var detail = options.MinutesPerBucket == _settings.CurrentValue.DrillDownMinutesPerBucket;
                var metrics = await _repo.GetMetrics(from, to, detail);

                UpdateTimeBucket(ref metrics, options);
                result = metrics;
            }

            if (!result.Any())
            {
                var records = await GetLogRecords(options);
                var groupped = records.GroupBy(i => i as TGroupKey);

                result = (IEnumerable<TMetrics>)groupFunction(groupped);
            }

            return result;
        }

        public async Task<long> SaveLogMetrics(DateOnly date, IEnumerable<TMetrics> metrics, bool detailedGroupping)
        {
            var rowsSaved = await _repo.SaveMetrics(metrics.Cast<TMetrics>(), detailedGroupping);

            return rowsSaved;
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
            var previousLineMatched = false;
            var backBuffer = new LinkedList<string>();
            var sb = new StringBuilder();

            foreach (var logLine in ReadLogs(date))
            {
                if (logLine.Contains(searchTerm))
                {
                    var match = mandatoryAttribute.Regex.Match(logLine);

                    if (match.Success)
                    {
                        keys.Add(match.Groups[keyAttribute.MatchGroup].Value);
                    }
                    else
                    {
                        //search term found on a line that does not match the mandatory pattern (typically exceptions)
                        //go back and find all matching lines from our back buffer
                        if (backBuffer.Any() && !previousLineMatched)
                        {
                            for (var element = backBuffer.Last; element != null; element = element.Previous)
                            {
                                var backBufferLine = element.Value;

                                match = mandatoryAttribute.Regex.Match(backBufferLine);

                                if (match.Success)
                                {
                                    keys.Add(match.Groups[keyAttribute.MatchGroup].Value);
                                    for (element = backBuffer.First; element != null;)
                                    {
                                        backBufferLine = element.Value;
                                        if (keys.Any(key => backBufferLine.Contains(key)))
                                        {
                                            var next = element.Next;

                                            //remove the matching line from our back buffer
                                            backBuffer.Remove(element);
                                            element = next;

                                            sb.AppendLine(backBufferLine);
                                        }
                                        else
                                        {
                                            element = element.Next;
                                        }
                                    }
                                    previousLineMatched = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (backBuffer.Count == _settings.CurrentValue.LogReaderBackBufferCapacity)
                {
                    backBuffer.RemoveFirst();
                }
                backBuffer.AddLast(logLine);

                if (keys.Any(key => logLine.Contains(key)))
                {
                    sb.AppendLine(logLine);
                    previousLineMatched = true;
                }
                else if (previousLineMatched)
                {
                    var match = mandatoryAttribute.Regex.Match(logLine);

                    if (match.Success)
                    {
                        //this line does not contain the search term, but it matches the mandatory pattern
                        //it belongs to a request that we do not care about
                        previousLineMatched = false;
                    }
                    else
                    {
                        //certain messages get logged on multiple lines (typically exceptions)
                        //in such case only the 1st line matches the regex
                        sb.AppendLine(logLine);
                    }
                }
            }

            return sb.ToString();
        }

        public string HighlightLogContent(string content, string searchTerm)
        {
            var props = typeof(TRecord).GetProperties();
            var mandatoryAttribute = typeof(TRecord).GetCustomAttributes(true)
                                                    .Single(i => i is MandatoryLogPatternAttribute) as MandatoryLogPatternAttribute;
            var patterns = props.Where(i => i.GetCustomAttributes(true)
                                             .Any(j => j is LogPatternAttribute))
                                .Select(i => i.GetCustomAttributes(true)
                                              .Single(j => j is LogPatternAttribute) as LogPatternAttribute);
            var sb = new StringBuilder();

            foreach (var line in content.Replace("\r\n", "\n").Split(new[] { '\r', '\n' }))
            {
                var highlightedLine = line;
                var mandatoryMatch = mandatoryAttribute.Regex.Match(line);

                if (mandatoryMatch.Success)
                {
                    highlightedLine = HighlightMatch(highlightedLine, mandatoryMatch, "mandatory patternMatch");

                    foreach (var pattern in patterns.OrderBy(i => i is ErrorLogPatternAttribute ? 0 : 1)
                                                    .ThenBy(i => i is WarningLogPatternAttribute ? 0 : 1))
                    {
                        //string.Contains is a fast pre-check
                        if (pattern.Filter == null || line.Contains(pattern.Filter))
                        {
                            //if pre-check suceeded try a regex match
                            var match = pattern.Regex.Match(line);

                            var @class = pattern is ErrorLogPatternAttribute
                                         ? "errorMatch"
                                         : pattern is WarningLogPatternAttribute
                                           ? "warningMatch"
                                           : null;
                            highlightedLine = HighlightMatch(highlightedLine, match, @class);
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(highlightedLine))
                {
                    highlightedLine = $"<span class=\"noMatch\">{highlightedLine}</span>";
                }

                sb.AppendLine(highlightedLine);
            }
            content = sb.ToString().Replace(searchTerm, $"<span class=\"highlight\">{searchTerm}</span>");

            return content;
        }

        private string HighlightMatch(string line, Match match, string @class = null)
        {
            if (match.Success)
            {
                @class ??= "patternMatch";

                if (match.Groups.Count == 1)
                {
                    line = line.Replace(match.Value, $"<span class=\"{@class}\"><span class=\"patternMatchValue\">{match.Value}</span></span>");
                }
                else
                {
                    var highlightedMatch = match.Value;

                    for (var i = 1; i < match.Groups.Count; i++)
                    {
                        highlightedMatch = highlightedMatch.Replace(match.Groups[i].Value, $"<span class=\"patternMatchValue\">{match.Groups[i].Value}</span>");
                    }
                    line = line.Replace(match.Value, $"<span class=\"{@class}\">{highlightedMatch}</span>");
                }
            }

            return line;
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
            foreach (var file in GetLogFilenames(date))
            {
                var logLines = ReadLinesWithoutLocking(file);

                foreach (var line in logLines)
                {
                    yield return line;
                }
            }
        }

        private static IEnumerable<string> ReadLinesWithoutLocking(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
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

        private static void UpdateTimeBucket(ref IEnumerable<TMetrics> metrics, LogReaderOptions options)
        {
            var props = typeof(TMetrics).GetProperties();
            var timebucketProp = props.Single(i => i.GetCustomAttributes(typeof(TimeBucketAttribute), false).Any());

            foreach (var item in metrics)
            {
                var time = (DateTime)timebucketProp.GetValue(item);
                var bucket = DateTimeToBucket(time, options);

                timebucketProp.SetValue(item, bucket);
            }
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

        public async Task<bool> HasLogMetrics(DateOnly date, bool detailedGroupping)
        {
            var from = new DateTime(date.Year, date.Month, date.Day);
            var to = from.AddDays(1);
            var b = await _repo.HasMetrics(from, to, detailedGroupping);

            return b;
        }

        public async Task<bool> DeleteLogMetrics(DateOnly date, bool detailedGroupping)
        {
            var from = new DateTime(date.Year, date.Month, date.Day);
            var to = from.AddDays(1);
            var b = await _repo.DeleteMetrics(from, to, detailedGroupping);

            return b;
        }
    }
}

