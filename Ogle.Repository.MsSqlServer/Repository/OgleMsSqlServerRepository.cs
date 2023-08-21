using Microsoft.Extensions.Options;
using Ogle.Repository.Sql.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogle.Repository.MsSqlServer
{
    public class OgleMsSqlServerRepository<TMetrics> : OgleSqlRepository<SqlConnection, TMetrics>
    {
        public OgleMsSqlServerRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings) : base(settings)
        {
        }

        #region Overriden methods

        public async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics, bool detailedGroupping)
        {
            var dt = new DataTable(Settings.CurrentValue.TableName);
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);

            foreach(var prop in props)
            {
                dt.Columns.Add(new DataColumn(prop.Name));
            }

            foreach(var row in metrics)
            {
                var values = new List<object>();

                foreach(var prop in props)
                {
                    values.Add(prop.GetValue(row));
                }
                dt.Rows.Add(values.ToArray());
            }

            var rowsInserted = 0L;

            using (var connection = new SqlConnection(Settings.CurrentValue.ConnectionString))
            {
                using (var bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = detailedGroupping? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
                    bulk.NotifyAfter = dt.Rows.Count;
                    bulk.SqlRowsCopied += (s, e) => rowsInserted += e.RowsCopied;

                    foreach(DataColumn column in dt.Columns)
                    {
                        bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await connection.OpenAsync();
                    await bulk.WriteToServerAsync(dt);
                }
            }

            return rowsInserted;
        }

        protected override string BuildCreateTableCommand(bool detailedTable)
        {
            var tableName = detailedTable ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sb = new StringBuilder($"IF OBJECT_ID(N'{tableName}') IS NULL CREATE TABLE {tableName} (_id INT IDENTITY PRIMARY KEY");
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);

            foreach (var prop in props)
            {
                var dbType = GetDbType(prop.PropertyType);

                sb.Append($", {prop.Name} {dbType}");
            }
            sb.Append(")");

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
                dbType = "BIT";
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
                dbType = "REAL";
            }
            else if (type == typeof(double) ||
                     type == typeof(double?))
            {
                dbType = "FLOAT";
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
                dbType = "UNIQUEIDENTIFIER";
            }
            else if (type == typeof(string))
            {
                dbType = "VARCHAR(MAX)";
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

