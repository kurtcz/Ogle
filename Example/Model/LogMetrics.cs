using System.ComponentModel;
using Ogle;

namespace Example.Model
{
	public class LogMetrics : LogGroupKey
	{
        [Total]
        [DisplayName("Total Requests")]
		public int TotalRequests { get; set; }

        [DisplayName("Successful Requests")]
        public int SuccessfulRequests { get; set; }

        [DisplayName("Failed Requests")]
        public int FailedRequests => TotalRequests - SuccessfulRequests;

        [Aggregate(AggregationOperation.Max)]
        [DisplayName("Max Requests In Flight")]
        public int MaxRequestsInFlight { get; set; }

        [Aggregate(AggregationOperation.Avg)]
        [DisplayName("Avg Items")]
        public int AvgItems { get; set; }

        [Aggregate(AggregationOperation.Min)]
        [DisplayName("Min Duration")]
        public TimeSpan MinDuration { get; set; }

        [Aggregate(AggregationOperation.Avg)]
        [DisplayName("Avg Duration")]
        public TimeSpan AvgDuration { get; set; }

        [Aggregate(AggregationOperation.Max)]
        [DisplayName("Max Duration")]
        public TimeSpan MaxDuration { get; set; }
    }
}

