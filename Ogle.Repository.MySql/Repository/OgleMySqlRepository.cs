using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Ogle.Repository.Sql.Abstractions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ogle.Repository.MySql
{
    public class OgleMySqlRepository<TMetrics> : OgleSqlRepository<MySqlConnection, TMetrics>
        where TMetrics : new()
    {
        static OgleMySqlRepository()
        {
            SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());
        }

        public OgleMySqlRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings) : base(settings)
        {
        }

        #region Overriden methods

        protected override string BuildCreateTableCommand(bool detailedTable)
        {
            var tableName = detailedTable ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY");

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
                dbType = "INT";
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
                dbType = "DECIMAL";
            }
            else if (type == typeof(float) ||
                        type == typeof(float?))
            {
                dbType = "FLOAT";
            }
            else if (type == typeof(double) ||
                        type == typeof(double?))
            {
                dbType = "DOUBLE";
            }
            else if (type == typeof(DateTime) ||
                        type == typeof(DateTime?))
            {
                dbType = "DATETIME";
            }
            else if (type == typeof(TimeSpan) ||
                        type == typeof(TimeSpan?))
            {
                dbType = "TIME";
            }
            else if (type == typeof(Guid) ||
                     type == typeof(Guid?))
            {
                dbType = "CHAR(36)";
            }
            else if (type == typeof(string))
            {
                if (maxLength >= 0 && maxLength <= 65535)
                {
                    dbType = $"VARCHAR({maxLength})";
                }
                else
                {
                    dbType = "VARCHAR(65535)";
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