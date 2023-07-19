using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Ogle;
using Ogle.Repository.Sql;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ogle.Repository.MySql
{
    public class OglePostgreSqlRepository<TMetrics> : OgleSqlRepository<NpgsqlConnection, TMetrics>
    {
        public OglePostgreSqlRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings) : base(settings)
        {
        }

        #region Overriden methods

        protected override string BuildCreateTableCommand()
        {
            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {Settings.CurrentValue.TableName} (_id SERIAL PRIMARY KEY");
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);

            foreach (var prop in props)
            {
                var dbType = GetDbType(prop.GetType());

                sb.Append($", {prop.Name} {dbType}");
            }
            sb.Append(");");

            return sb.ToString();
        }

        #endregion

        #region Private methods

        private static string GetDbType(Type type)
        {
            string dbType;

            if (type == typeof(bool) ||
                type == typeof(bool?))
            {
                dbType = "BOOL";
            }
            else if (type == typeof(short) ||
                     type == typeof(ushort) ||
                     type == typeof(short?) ||
                     type == typeof(ushort?))
            {
                dbType = "SMALLINT";
            }
            else if (type == typeof(int) ||
                     type == typeof(uint) ||
                     type == typeof(int?) ||
                     type == typeof(uint?))
            {
                dbType = "INTEGER";
            }
            else if (type == typeof(long) ||
                     type == typeof(ulong) ||
                     type == typeof(long?) ||
                     type == typeof(ulong?))
            {
                dbType = "BIGINT";
            }
            else if (type == typeof(decimal) ||
                     type == typeof(decimal?))
            {
                dbType = "NUMERIC";
            }
            else if (type == typeof(float) ||
                     type == typeof(float?))
            {
                dbType = "REAL";
            }
            else if (type == typeof(double) ||
                     type == typeof(double?))
            {
                dbType = "DOUBLE PRECISION";
            }
            else if (type == typeof(DateTime) ||
                     type == typeof(DateTime?))
            {
                dbType = "TIMESTAMP";
            }
            else if (type == typeof(TimeSpan) ||
                     type == typeof(TimeSpan?))
            {
                dbType = "TIME";
            }
            else if (type == typeof(Guid) ||
                     type == typeof(Guid?))
            {
                dbType = "UUID";
            }
            else if (type == typeof(string))
            {
                dbType = "TEXT";
            }
            else
            {
                throw new InvalidOperationException($"No DbType defined for .NET type {type.Name}");
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                dbType += " NOT NULL";
            }

            return dbType;
        }

        #endregion
    }
}