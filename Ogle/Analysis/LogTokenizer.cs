using System.IO;
using J2N;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Ogle
{
    public sealed class LogTokenizer : CharTokenizer
    {
        public LogTokenizer(LuceneVersion luceneVersion, TextReader input)
            : base(luceneVersion, input)
        {
        }

        protected override bool IsTokenChar(int c)
        {
            return Character.IsLetterOrDigit(c);
        }
    }
}

