using Dapper;
using Microsoft.Extensions.Options;
using Ogle;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Ogle.Repository.MsSqlServer
{
    public class OgleMsSqlServerRepository<TMetrics> : ILogMetricsRepository<TMetrics>
    {
        private IOptionsMonitor<OgleMsSqlRepositoryOptions> _settings;

        public OgleMsSqlServerRepository(IOptionsMonitor<OgleMsSqlRepositoryOptions> settings)
        {
            _settings = settings;
        }

        public async Task<IEnumerable<TMetrics>> GetMetrics(DateTime from, DateTime to)
        {
            using(var connection = new SqlConnection())
            {
                var sql = BuildSelectCommand();
                var result = await connection.QueryAsync<TMetrics>(sql, new { from, to });

                return result;
            }
        }

        public async Task<long> SaveMetrics(IEnumerable<TMetrics> metrics)
        {
            var dt = new DataTable(_settings.CurrentValue.TableName);
            var props = typeof(TMetrics).GetProperties();

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

            using (var connection = new SqlConnection(_settings.CurrentValue.ConnectionString))
            {
                using (var bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = _settings.CurrentValue.TableName;
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

        private string BuildSelectCommand()
        {
            var sql = new StringBuilder("SELECT ");
            var props = typeof(TMetrics).GetProperties();
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
    }
}

