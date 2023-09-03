using System;
using Ogle;

namespace Example.Model
{
	public class LogRecord : LogGroupKey
	{
		[Mandatory(3, isKey: true)]
		public string RequestId { get; set; }

		[LogPattern("flight", @"(\d+)\ requests\ in\ flight")]
		public int RequestsInFlight { get; set; }

		[LogPattern("Found", @"Found\ (\d+)\ \S+\ in\ the\ database")]
		public int Items { get; set; }

        [LogPattern("Returning", @"Returning\ (2\d{2}\ \S+)")]
        public bool Succeeded { get; set; }

        [LogPattern("Returning", @"Returning\ ([^2]\d{2}\ \S+)")]
        public bool Failed { get; set; }

        [LogPattern("completed", @"Request\ completed\ in\ (\d{2}\:\d{2}\:\d{2}\.\d{3})", @"hh\:mm\:ss\.fff")]
        public TimeSpan Duration { get; set; }

        [WarningLogPattern("WRN", @"\ ([^;]+)$")]
        public string Warning { get; set; }

        [ErrorLogPattern("ERR", @"\ ([^;]+)$")]
		public string Error { get; set; }
    }
}

