using System;
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

        //it is vital to override GetHashCode and Equals so that groupping of LogRecord items works as expected
        public override int GetHashCode()
        {
            return HashCode.Combine(ServerName?.ToLowerInvariant(), UserName?.ToLowerInvariant(), Endpoint?.ToLowerInvariant(), Timestamp);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LogGroupKey);
        }

        public bool Equals(LogGroupKey? other)
        {
            return other != null &&
                   string.Equals(ServerName, other.ServerName, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(UserName, other.UserName, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(Endpoint, other.Endpoint, StringComparison.InvariantCultureIgnoreCase) &&
                   Timestamp == other.Timestamp;
        }
    }
}

