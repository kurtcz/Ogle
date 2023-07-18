﻿using Dapper;
using Microsoft.Extensions.Options;
using Ogle;
using Ogle.Repository.SQLite;
using System.Data;
using System.Data.SQLite;
using System.Reflection;
using System.Text;

namespace Ogle.Repository.SQLite
{
    public class OgleSQLiteRepository<TMetrics> : ILogMetricsRepository<TMetrics>
    {
        private IOptionsMonitor<OgleSQLiteRepositoryOptions> _settings;

        static OgleSQLiteRepository()
        {
            SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(TimeSpan), new TimeSpanTypeHandler());
        }

        public OgleSQLiteRepository(IOptionsMonitor<OgleSQLiteRepositoryOptions> settings)
        {
            _settings = settings;
        }

        #region ILogMetricsRepository methods

        public async Task<bool> HasMetrics(DateTime from, DateTime to)
        {
            using (var connection = new SQLiteConnection(_settings.CurrentValue.ConnectionString))
            {
                connection.Open();

                var sql = BuildCountCommand();
                var count = (long)await connection.ExecuteScalarAsync(sql, new { from, to });

                return count > 0;
            }
        }

        public async Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to)
        {
            using(var connection = new SQLiteConnection(_settings.CurrentValue.ConnectionString))
            {
                var sql = BuildSelectCommand();
                var result = await connection.QueryAsync<TMetrics>(sql, new { from, to });

                return result;
            }
        }

        public async Task<bool> DeleteMetrics(DateTime from, DateTime to)
        {
            using (var connection = new SQLiteConnection(_settings.CurrentValue.ConnectionString))
            {
                connection.Open();

                var sql = BuildDeleteCommand();
                var deleted = await connection.ExecuteAsync(sql, new { from, to });

                return deleted > 0;
            }
        }

        public async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics)
        {
            var dt = new DataTable(_settings.CurrentValue.TableName);
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite); 

            foreach(var prop in props)
            {
                dt.Columns.Add(new DataColumn(prop.Name));
            }

            var rowsInserted = 0L;

            using (var connection = new SQLiteConnection(_settings.CurrentValue.ConnectionString))
            {
                connection.Open();
                
                var sql = BuildInsertCommand();

                foreach (var row in metrics)
                {
                    await connection.ExecuteAsync(sql, row);
                    rowsInserted++;
                }
            }

            return rowsInserted;
        }

        #endregion

        #region Private methods

        private string BuildSelectCommand()
        {
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
            sql.Append($" FROM {_settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to");

            return sql.ToString();
        }

        private string BuildInsertCommand()
        {
            var sql = new StringBuilder($"INSERT INTO {_settings.CurrentValue.TableName} (");
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

        private string BuildDeleteCommand()
        {
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);
            var sql = $"DELETE FROM {_settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        private string BuildCountCommand()
        {
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);
            var sql = $"SELECT COUNT(*) FROM {_settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        #endregion
    }
}
