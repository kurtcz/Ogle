using Microsoft.Extensions.Options;
using Npgsql;
using Ogle.Repository.Sql.Abstractions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ogle.Repository.PostgreSql
{
    public class OglePostgreSqlRepository<TMetrics> : OgleSqlRepository<NpgsqlConnection, TMetrics>
        where TMetrics : new()
    {
        public OglePostgreSqlRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings) : base(settings)
        {
        }

        #region Overriden methods

        protected override string BuildCreateTableCommand(bool detailedTable)
        {
            var tableName = detailedTable ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (_id SERIAL PRIMARY KEY");

            foreach (var prop in PropertyInfos)
            {
                var dbType = GetDbType(prop);

                sb.Append($", {prop.Name} {dbType}");
            }
            sb.Append(");");

            return sb.ToString();
        }

        #endregion

        #region Private methods

        private static string GetDbType(PropertyInfo propertyInfo)
        {
            Type type = propertyInfo.PropertyType;
            int maxLength = propertyInfo.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? -1;
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
                if (maxLength >= 0)
                {
                    dbType = $"VARCHAR({maxLength})";
                }
                else
                {
                    dbType = "TEXT";
                }
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