using System;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MandatoryAttribute : Attribute
	{
		public int MatchGroup { get; }
		public bool IsKey { get; }
		public string? SearchPattern { get; }
		public string? Format { get; }

		public MandatoryAttribute(int matchGroup) : this(matchGroup, false, null, null)
		{
		}

		public MandatoryAttribute(int matchGroup, bool isKey, string? searchPattern) : this(matchGroup, isKey, searchPattern, null)
		{
		}

		public MandatoryAttribute(int matchGroup, bool isKey, string? searchPattern, string? format)
		{
			MatchGroup = matchGroup;
			IsKey = isKey;
			SearchPattern = searchPattern;
			Format = format;
		}
	}
}

