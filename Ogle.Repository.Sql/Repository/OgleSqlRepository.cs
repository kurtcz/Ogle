using Dapper;
using Microsoft.Extensions.Options;
using Ogle;
using Ogle.Repository.Sql;
using System.Data;
using System.Reflection;
using System.Text;

namespace Ogle.Repository.Sql
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

        public virtual async Task<bool> HasMetrics(DateTime from, DateTime to)
        {
            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTableIfNeeded();

                var sql = BuildCountCommand();
                var count = (long)await connection.ExecuteScalarAsync(sql, new { from, to });

                return count > 0;
            }
        }

        public virtual async Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to)
        {
            using(var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTableIfNeeded();

                var sql = BuildSelectCommand();
                var result = await connection.QueryAsync<TMetrics>(sql, new { from, to });

                return result;
            }
        }

        public virtual async Task<bool> DeleteMetrics(DateTime from, DateTime to)
        {
            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                await CreateTableIfNeeded();

                var sql = BuildDeleteCommand();
                var deleted = await connection.ExecuteAsync(sql, new { from, to });

                return deleted > 0;
            }
        }

        public virtual async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics)
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

                await CreateTableIfNeeded();

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

        public virtual async Task CreateTableIfNeeded()
        {
            if (!Settings.CurrentValue.AutoCreateTable)
            {
                return;
            }

            using (var connection = new TDbConnection())
            {
                connection.ConnectionString = Settings.CurrentValue.ConnectionString;

                var sql = BuildCreateTableCommand();
                var result = await connection.ExecuteAsync(sql);
            }
        }

        protected abstract string BuildCreateTableCommand();

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
            sql.Append($" FROM {Settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to");

            return sql.ToString();
        }

        private string BuildInsertCommand()
        {
            var sql = new StringBuilder($"INSERT INTO {Settings.CurrentValue.TableName} (");
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
            var sql = $"DELETE FROM {Settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        private string BuildCountCommand()
        {
            var props = typeof(TMetrics).GetProperties().Where(i => i.CanWrite);
            var timeBucketProp = props.Single(i => i.GetCustomAttribute(typeof(TimeBucketAttribute)) != null);
            var sql = $"SELECT COUNT(*) FROM {Settings.CurrentValue.TableName} WHERE {timeBucketProp.Name} >= @from AND {timeBucketProp.Name} < @to";

            return sql;
        }

        #endregion
    }
}

