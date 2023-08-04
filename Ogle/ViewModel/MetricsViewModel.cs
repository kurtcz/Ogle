using System;

namespace Ogle
{
	public class MetricsViewModel
	{
		public string[] KeyProperties { get; set; }
        public string[] KeyPropertyDisplayNames { get; set; }
        public string[] ValueProperties { get; set; }
        public string[] ValuePropertyTypes { get; set; }
        public string[] ValuePropertyDisplayNames { get; set; }
        public string[] ValuePropertyAggregationOperation { get; set; }
        public string TotalProperty { get; set; }
        public string TimeBucketProperty { get; set; }
        public string[] TimeBuckets { get; set; }
        public string[] ServerUrls { get; set; }
        public DateOnly Date { get; set; }
        public int HourFrom { get; set; }
        public int MinuteFrom { get; set; }
        public int MinutesPerBucket { get; set; }
        public int NumberOfBuckets { get; set; }
        public bool AutoFetchData { get; set; }
        public bool CanDrillDown { get; set; }
        public int DrillDownMinutesPerBucket { get; set; }
        public int DrillDownNumberOfBuckets { get; set; }
    }
}

