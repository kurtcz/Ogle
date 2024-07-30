using System;

namespace Ogle
{
    public enum RegexSource
    {
        LogPattern,
        FilenamePattern
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MandatoryAttribute : Attribute
    {
        public int MatchGroup { get; }
        public bool IsKey { get; }
        public RegexSource RegexSource { get; }
        public string? Format { get; }

        public MandatoryAttribute(int matchGroup) : this(matchGroup, false, RegexSource.LogPattern, null)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey) : this(matchGroup, isKey, RegexSource.LogPattern, null)
        {
        }

        public MandatoryAttribute(int matchGroup, RegexSource regexSource) : this(matchGroup, false, regexSource, null)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey, string? format) : this(matchGroup, isKey, RegexSource.LogPattern, format)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey, RegexSource regexSource) : this(matchGroup, isKey, regexSource, null)
        {
        }

        public MandatoryAttribute(int matchGroup, RegexSource regexSource, string? format) : this(matchGroup, false, regexSource, format)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey, RegexSource regexSource, string? format)
        {
            MatchGroup = matchGroup;
            IsKey = isKey;
            RegexSource = regexSource;
            Format = format;
        }
    }
}

