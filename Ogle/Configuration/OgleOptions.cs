using System;
using System.Collections.Generic;
using System.Linq;

namespace Ogle
{
    public class OgleOptions
    {
        public string LogFolder { get; set; }
        public string LogFilePattern { get; set; }
        public string AllowedSearchPattern { get; set; }
        public int LogReaderBackBufferCapacity { get; set; }
        public int HttpPort { get; set; }
        public int HttpsPort { get; set; }
        public string Layout { get; set; }
        public string[] Hostnames { get; set; }
        public string[] DatasetColors { get; set; }
        public MetricsButtonsPosition MetricsButtonsPosition { get; set; }
        public FilterControlsPosition FilterControlsPosition { get; set; }
        public int DefaultMinutesPerBucket { get; set; }
        public int DefaultNumberOfBuckets { get; set; }
        public int DrillDownMinutesPerBucket { get; set; }
        public int DrillDownNumberOfBuckets { get; set; }
        public TimeSpan LogParserTimeout { get; set; }
        public Type GroupKeyType { get; set; }
        public Type RecordType { get; set; }
        public Type MetricsType { get; set; }
        public Func<IEnumerable<IGrouping<object, object>>, object> GroupFunction { get; set; }
    }
}

