using Dapper;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Example.Model;
using Microsoft.Extensions.Options;
using Ogle;
using Ogle.Repository.SQLite;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddOgleSQLiteRepository<LogMetrics>(builder.Configuration.GetSection("Ogle:RepositorySettings"));
            builder.Services.AddOgle(builder.Configuration.GetSection("Ogle"), options =>
            {
                options.GroupKeyType = typeof(LogGroupKey);
                options.RecordType = typeof(LogRecord);
                options.MetricsType = typeof(LogMetrics);
                options.GroupFunction = grouppedRecords => grouppedRecords.Select(i =>
                {
                    var g = (IGrouping<LogGroupKey,LogRecord>)i;

                    return new LogMetrics
                    {
                        ServerName = g.Key.ServerName,
                        UserName = g.Key.UserName,
                        Endpoint = g.Key.Endpoint,
                        Timestamp = g.Key.Timestamp,
                        TotalRequests = g.Count(),
                        SuccessfulRequests = g.Count(j => j.Succeeded),
                        MaxRequestsInFlight = g.Max(j => j.RequestsInFlight),
                        AvgItems = (int)Math.Round(g.Average(j => j.Items)),
                        MinDuration = g.Min(j => j.Duration),
                        AvgDuration = TimeSpan.FromTicks((int)Math.Round(g.Average(j => j.Duration.Ticks))),
                        MaxDuration = g.Max(j => j.Duration)
                    };
                });
            });
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            ConfigureDatabase(builder.Services);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllers();
            app.MapRazorPages();

            app.Run();
        }

        private static void ConfigureDatabase(IServiceCollection serviceCollection)
        {
            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                var settings = serviceProvider.GetRequiredService<IOptionsMonitor<OgleSQLiteRepositoryOptions>>();

                using (var connection = new SQLiteConnection(settings.CurrentValue.ConnectionString))
                {
                    connection.Open();

                    var sql = BuildCreateTableCommand(settings.CurrentValue.TableName);

                    connection.Execute(sql);
                }
            }
        }

        private static string BuildCreateTableCommand(string tableName)
        {
            var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {tableName} (id INTEGER PRIMARY KEY");
            var props = typeof(LogMetrics).GetProperties().Where(i => i.CanWrite);

            foreach(var prop in props)
            {
                var dbType = GetSQLiteDbType(prop.GetType());

                sb.Append($", {prop.Name} {dbType}");
            }
            sb.Append(");");

            return sb.ToString();
        }

        private static string GetSQLiteDbType(Type type)
        {
            if(type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(long) ||
               type == typeof(ulong))
            {
                return "INTEGER";
            }

            return "TEXT";
        }
    }
}