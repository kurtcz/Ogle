using System;
using System.Text.RegularExpressions;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FilenamePatternAttribute : Attribute
    {
        public Regex Regex { get; }

        public FilenamePatternAttribute(string regex)
        {
            Regex = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);
        }
    }
}

