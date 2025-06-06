using System;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AggregateAttribute : Attribute
    {
        public AggregationOperation AggregationOperation { get; }

        public AggregateAttribute(AggregationOperation aggregationOperation)
        {
            AggregationOperation = aggregationOperation;
        }
    }
}

