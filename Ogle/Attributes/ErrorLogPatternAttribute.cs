namespace Ogle
{
    public class ErrorLogPatternAttribute : LogPatternAttribute
    {
        public ErrorLogPatternAttribute(string regex) : base(regex)
        {
        }

        public ErrorLogPatternAttribute(string? filter, string regex) : base(filter, regex)
        {
        }

        public ErrorLogPatternAttribute(string regex, int matchGroup) : base(regex, matchGroup)
        {
        }

        public ErrorLogPatternAttribute(string regex, int matchGroup, string? format) : base(regex, matchGroup, format)
        {
        }

        public ErrorLogPatternAttribute(string? filter, string regex, int matchGroup) : base(filter, regex, matchGroup)
        {
        }

        public ErrorLogPatternAttribute(string? filter, string regex, string? format) : base(filter, regex, format)
        {
        }

        public ErrorLogPatternAttribute(string? filter, string regex, string? format, int matchGroup) : base(filter, regex, format, matchGroup)
        {
        }
    }
}

