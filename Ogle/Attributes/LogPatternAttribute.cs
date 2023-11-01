using System;
using System.Text.RegularExpressions;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LogPatternAttribute : Attribute
    {
        public string? Filter { get; }
        public Regex Regex { get; }
        public string? Format { get; }
        public int MatchGroup { get; }

        public LogPatternAttribute(string regex) : this(null, regex, null, 1)
        {
        }

        public LogPatternAttribute(string? filter, string regex) : this(filter, regex, null, 1)
        {
        }

        public LogPatternAttribute(string regex, int matchGroup) : this(null, regex, null, matchGroup)
        {
        }

        public LogPatternAttribute(string regex, int matchGroup, string? format) : this(null, regex, format, matchGroup)
        {
        }

        public LogPatternAttribute(string? filter, string regex, int matchGroup) : this(filter, regex, null, matchGroup)
        {
        }

        public LogPatternAttribute(string? filter, string regex, string? format) : this(filter, regex, format, 1)
        {
        }

        public LogPatternAttribute(string? filter, string regex, string? format, int matchGroup)
        {
            Filter = filter;
            Regex = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);
            Format = format;
            MatchGroup = matchGroup;
        }
    }
}

