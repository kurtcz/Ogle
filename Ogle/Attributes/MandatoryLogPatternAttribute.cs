using System;
using System.Text.RegularExpressions;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MandatoryLogPatternAttribute : Attribute
    {
        public Regex Regex { get; }

        public MandatoryLogPatternAttribute(string regex)
        {
            Regex = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);
        }
    }
}

