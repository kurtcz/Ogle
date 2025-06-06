using System.IO;
using System.Linq;
using J2N;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Ogle
{
    public sealed class LogTokenizer : CharTokenizer
    {
        private readonly char[]? _separators;

        public LogTokenizer(LuceneVersion luceneVersion, TextReader input, char[]? separators = null)
            : base(luceneVersion, input)
        {
            _separators = separators;
        }

        protected override bool IsTokenChar(int c)
        {
            if (_separators != null)
            {
                return _separators.All(i => i != c);
            }
            return Character.IsLetterOrDigit(c);
        }
    }
}

