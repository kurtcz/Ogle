using System;

namespace Ogle
{
	public class OgleOptions
	{
		public string LogFolder { get; set; }
		public string LogFilePattern { get; set; }
		public int HttpPort { get; set; }
        public int HttpsPort { get; set; }
		public string[] ServerUrls { get; set; }
		public string[] DatasetColors { get; set; }
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

