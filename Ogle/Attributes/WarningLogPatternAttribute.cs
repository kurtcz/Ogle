namespace Ogle
{
    public class WarningLogPatternAttribute : LogPatternAttribute
    {
        public WarningLogPatternAttribute(string regex) : base(regex)
        {
        }

        public WarningLogPatternAttribute(string? filter, string regex) : base(filter, regex)
        {
        }

        public WarningLogPatternAttribute(string regex, int matchGroup) : base(regex, matchGroup)
        {
        }

        public WarningLogPatternAttribute(string regex, int matchGroup, string? format) : base(regex, matchGroup, format)
        {
        }

        public WarningLogPatternAttribute(string? filter, string regex, int matchGroup) : base(filter, regex, matchGroup)
        {
        }

        public WarningLogPatternAttribute(string? filter, string regex, string? format) : base(filter, regex, format)
        {
        }

        public WarningLogPatternAttribute(string? filter, string regex, string? format, int matchGroup) : base(filter, regex, format, matchGroup)
        {
        }
    }
}

