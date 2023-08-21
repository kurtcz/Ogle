using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ogle.Repository.Sql.Abstractions
{
    public abstract class OgleSqlRepository<TDbConnection, TMetrics> : ILogMetricsRepository<TMetrics>
        where TDbConnection : IDbConnection, new()
    {
        protected IOptionsMonitor<OgleSqlRepositoryOptions> Settings { get; }

        public OgleSqlRepository(IOptionsMonitor<OgleSqlRepositoryOptions> settings)
        {
            Settings = settings;
        }

        #region ILogMetricsRepository methods

        public virtual async Task<bool> HasMetrics(DateTime from, DateTime to, bool detailedTable)
        {
            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTablesIfNeeded();

                var sql = BuildCountCommand(detailedTable);
                var count = Convert.ToInt64(await connection.ExecuteScalarAsync(sql, new { from, to }));

                return count > 0;
            }
        }

        public virtual async Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to, bool detailedTable)
        {
            using(var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTablesIfNeeded();

                var sql = BuildSelectCommand(detailedTable);
                var result = await connection.QueryAsync<TMetrics>(sql, new { from, to });

                return result;
            }
        }

        public virtual async Task<bool> DeleteMetrics(DateTime from, DateTime to, bool detailedTable)
        {
            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTablesIfNeeded();

                var sql = BuildDeleteCommand(detailedTable);
                var deleted = await connection.ExecuteAsync(sql, new { from, to });

                return deleted > 0;
            }
        }

        public virtual async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics, bool detailedTable)
        {
            var dt = new DataTable(Settings.CurrentValue.TableName);
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite); 

            foreach(var prop in props)
            {
                dt.Columns.Add(new DataColumn(prop.Name));
            }

            var rowsInserted = 0L;

            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTablesIfNeeded();

                var sql = BuildInsertCommand(detailedTable);

                foreach (var row in metrics)
                {
                    await connection.ExecuteAsync(sql, row);
                    rowsInserted++;
                }
            }

            return rowsInserted;
        }

        #endregion

        public virtual async Task CreateTablesIfNeeded()
        {
            if (!Settings.CurrentValue.AutoCreateTable)
            {
                return;
            }

            var detailedTableModes = new[] { false, true };

            foreach (var detailedTableMode in detailedTableModes)
            {
                using (var connection = new TDbConnection())
                {
                    connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                    var sql = BuildCreateTableCommand(detailedTableMode);
                    var result = await connection.ExecuteAsync(sql);
                }
            }
        }

        protected abstract string BuildCreateTableCommand(bool detailedTable);

        #region Private methods

        private string BuildSelectCommand(bool detailedGroupping)
        {
            var tableName = detailedGroupping ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sql = new StringBuilder("SELECT ");
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);

            var i = 0;
            foreach(var prop in props)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append(prop.Name);
                i++;
            }
            sql.Append($" FROM {tableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to");

            return sql.ToString();
        }

        private string BuildInsertCommand(bool detailedGroupping)
        {
            var tableName = detailedGroupping ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var sql = new StringBuilder($"INSERT INTO {tableName} (");
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite).ToArray();
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);

            var i = 0;
            foreach (var prop in props)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append(prop.Name);
                i++;
            }
            sql.Append(") VALUES (");

            i = 0;
            foreach (var prop in props)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append($"@{prop.Name}");
                i++;
            }
            sql.Append(");");

            return sql.ToString();
        }

        private string BuildDeleteCommand(bool detailedGroupping)
        {
            var tableName = detailedGroupping ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);
            var sql = $"DELETE FROM {tableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        private string BuildCountCommand(bool detailedGroupping)
        {
            var tableName = detailedGroupping ? Settings.CurrentValue.DetailedTableName : Settings.CurrentValue.TableName;
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);
            var sql = $"SELECT COUNT(*) FROM {tableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        #endregion
    }
}

