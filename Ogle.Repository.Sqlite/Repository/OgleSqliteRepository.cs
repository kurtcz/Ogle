using Dapper;
using Microsoft.Extensions.Options;
using Ogle.Repository.Sql.Abstractions;
using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace Ogle.Repository.Sqlite
{
    public class OgleSqliteRepository<TMetrics> : OgleSqlRepository<SQLiteConnection, TMetrics>
    {
        static OgleSqliteRepository()
        {
            SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanTypeHandler());
        }

        public OgleSqliteRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings) : base(settings)
        {
        }

        #region Overriden methods

        protected override string BuildCreateTableCommand(bool detailedTable)
        {
            var tableName = detailedTable ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (_id INTEGER PRIMARY KEY");
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);

            foreach (var prop in props)
            {
                var dbType = GetDbType(prop.PropertyType);

                sb.Append($", {prop.Name} {dbType}");
            }
            sb.Append(");");

            return sb.ToString();
        }

        #endregion

        #region Private methods

        private static string GetDbType(Type type)
        {
            if (type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong))
            {
                return "INTEGER";
            }

            return "TEXT";
        }

        #endregion
    }
}

