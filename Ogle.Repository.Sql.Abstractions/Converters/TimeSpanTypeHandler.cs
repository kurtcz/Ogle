using System;
using System.Data;
using Dapper;

namespace Ogle.Repository.Sql.Abstractions
{
    public class TimeSpanTypeHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
        {
            return TimeSpan.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.Value = value.ToString();
        }
    }
}

