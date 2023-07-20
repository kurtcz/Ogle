using System;
using System.Linq;
using Example.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ogle;
using Ogle.Repository.Sqlite;

namespace Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
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
            builder.Services.AddOgleSqliteRepository<LogMetrics>(builder.Configuration.GetSection("Ogle:RepositorySettings"));
            
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();

            var app = builder.Build();

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
    }
}