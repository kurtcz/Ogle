using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Microsoft.Extensions.Options;

namespace Ogle
{
    public class LogAnalyzer : StopwordAnalyzerBase
    {
        private readonly int _minFulltextTokenLength;
        private readonly int _maxFulltextTokenLength;
        private readonly char[]? _fulltextTokenSeparators;
        private static readonly CharArraySet ENGLISH_STOP_WORDS_SET = LoadEnglishStopwordSet();

        private static CharArraySet LoadEnglishStopwordSet()
        {
            IList<string> stopWords = new[] { "a", "an", "and", "are", "as", "at", "be",
                "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on",
                "or", "such", "that", "the", "their", "then", "there", "these", "they", "this",
                "to", "was", "will", "with" };
            var stopSet = new CharArraySet(LuceneVersion.LUCENE_CURRENT, stopWords, true);

            return CharArraySet.UnmodifiableSet(stopSet);
        }

        public LogAnalyzer(LuceneVersion luceneVersion, IOptionsMonitor<OgleOptions> settings)
            : this(luceneVersion, ENGLISH_STOP_WORDS_SET, settings)
        {
        }

        public LogAnalyzer(LuceneVersion luceneVersion, CharArraySet stopWords, IOptionsMonitor<OgleOptions> settings)
            :base(luceneVersion, stopWords)
        {
            _minFulltextTokenLength = settings.CurrentValue.MinFulltextTokenLength;
            _maxFulltextTokenLength = settings.CurrentValue.MaxFulltextTokenLength;
            _fulltextTokenSeparators = settings.CurrentValue.FulltextTokenSeparators?.ToCharArray();
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new LogTokenizer(m_matchVersion, reader, _fulltextTokenSeparators);
            TokenStream tokenStream;

            tokenStream = new LengthFilter(m_matchVersion, source, _minFulltextTokenLength, _maxFulltextTokenLength);
            tokenStream = new StopFilter(m_matchVersion, tokenStream, m_stopwords);

            return new TokenStreamComponents(source, tokenStream);
        }
    }
}

