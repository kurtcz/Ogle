using System.ComponentModel;
using Ogle;

namespace Example.Model
{
    [MandatoryLogPattern(@"^\[(\d{2}\:\d{2}\:\d{2})\ [A-Z]{3}\]\ ([^;]*);\ ReqId\=([^;]+);")]
	public class LogGroupKey
	{
        [Mandatory(2)]
        [DisplayName("Server")]
		public string ServerName { get; set; }

        [LogPattern("User", @"User\ '([^']+)'")]
        [DisplayName("User")]
        public string UserName { get; set; }

        [LogPattern("Request received", @"endpoint\:\ [A-Z]+\ (.+)$")]
        public string Endpoint { get; set; }

        [Mandatory(1)]
        [TimeBucket]
        public DateTime Timestamp { get; set; }
    }
}

