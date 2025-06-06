using System;

namespace Ogle
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChartOptionsAttribute : Attribute
    {
        public static ChartOptionsAttribute Default = new ChartOptionsAttribute(ChartType.Line);

        public ChartType ChartType { get; }
        public float Tension { get; }

        public ChartOptionsAttribute(ChartType chartType, float tension = 0.1f)
        {
            ChartType = chartType;
            Tension = tension;
        }
    }
}

