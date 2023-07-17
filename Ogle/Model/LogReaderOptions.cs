using System;

namespace Ogle
{
	public class LogReaderOptions
	{
		public DateOnly? Date { get; set; }
		public int HourFrom { get; set; }
		public int MinuteFrom { get; set; }
		public int? MinutesPerBucket { get; set; }
		public int? NumberOfBuckets { get; set; }
	}
}

