using System;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MandatoryAttribute : Attribute
    {
        public int MatchGroup { get; }
        public bool IsKey { get; }
        public string? Format { get; }

        public MandatoryAttribute(int matchGroup) : this(matchGroup, false, null)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey) : this(matchGroup, isKey, null)
        {
        }

        public MandatoryAttribute(int matchGroup, bool isKey, string? format)
        {
            MatchGroup = matchGroup;
            IsKey = isKey;
            Format = format;
        }
    }
}

