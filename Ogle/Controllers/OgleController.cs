﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ogle.Converters;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Ogle
{
	public class OgleController : Controller
	{
		private readonly ILogger<OgleController> _logger;
		private readonly IOptionsMonitor<OgleOptions> _settings;
		private readonly IServiceProvider _serviceProvider;

		public OgleController(ILogger<OgleController> logger, IOptionsMonitor<OgleOptions> settings, IServiceProvider serviceProvider)
		{
			_logger = logger ?? NullLogger<OgleController>.Instance;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #region Public endpoints

        [HttpGet]
        [Route("/ogle/")]
        [Route("/ogle/Logs")]
        [Route("/ogle/Index")]
        public IActionResult Index(string? id, string? hostName, DateTime? date)
		{
			date ??= DateTime.Today.AddDays(-1);
            try
            {
				var model = new LogsViewModel
				{
					RoutePrefix = GetRoutePrefix(),
					Id = id,
					Date = DateOnly.FromDateTime(date.Value),
					HostName = hostName,
					ServerSelectList = _settings.CurrentValue.Hostnames.Select(i => new SelectListItem
					{
						Text = i,
						Value = GetHostnameUrl(Request.Scheme, i).ToString()
					}).ToList()
                };

                return View("Logs", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
		}

		[HttpGet]
        [Route("/ogle/BrowseLogFiles")]
        public IActionResult BrowseLogFiles(DateTime? date)
		{
            try
            {
                dynamic logService = LogServiceFactory.CreateInstance(_settings);
				var sb = new StringBuilder("<pre>\n<b>Filename\t\t\t\tLast write time:\t\tFile size</b>\n");
				DateOnly? dateOnly = date.HasValue ? DateOnly.FromDateTime(date.Value) : null;

				foreach(var path in logService.GetLogFilenames(dateOnly))
				{
					var fi = new FileInfo(path);
					var filename = Path.GetFileName(path);
					var url = Url.Action("DownloadLog", "ogle", new { log = filename }, Request.Scheme, Request.Host.ToUriComponent());

					sb.Append($"<a href=\"{url}\">{filename}</a>\t\t{fi.LastWriteTime}\t{fi.Length,12:N0}\n");
				}
				sb.Append("</pre>\n");

				return Content(sb.ToString(), "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetLogs")]
        public async Task<IActionResult> GetLogs(DateTime? date, string id)
		{
            try
            {
				date ??= DateTime.Today.AddDays(-1);

				var searchPatternMatch = new Regex(_settings.CurrentValue.AllowedSearchPattern).Match(id);
                string result;
				
				if(id == null)
				{
					result = "Please specify a request id or a unique search term";
				}
				else if(!searchPatternMatch.Success)
				{
                    result = $"Search term has to match {_settings.CurrentValue.AllowedSearchPattern} regular expresion pattern";
                }
				else
				{
                    dynamic logService = LogServiceFactory.CreateInstance(_settings);

                    result = await (Task<string>)logService.GetLogContent(id, DateOnly.FromDateTime(date.Value));

					if (string.IsNullOrEmpty(result))
					{
						result = $"Request id or search term not found in logs for {date:yyyy-MM-dd}";
					}
				}

				return Content(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/DownloadLog")]
        public IActionResult DownloadLog(string log)
		{
            try
            {
                dynamic logService = LogServiceFactory.CreateInstance(_settings);
				var stream = logService.GetFileStreamWithoutLocking(log);

				//asp.net core will take care of disposing the stream
				return File(stream, "text/plain", log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetRecords")]
        public async Task<IActionResult> GetRecords(DateTime? date)
		{
            try
            {
				date ??= DateTime.Today.AddDays(-1);

				var options = new LogReaderOptions
				{
					Date = DateOnly.FromDateTime(date.Value),
					MinutesPerBucket = _settings.CurrentValue.DefaultMinutesPerBucket,
					NumberOfBuckets = _settings.CurrentValue.DefaultNumberOfBuckets
				};
				dynamic logService = LogServiceFactory.CreateInstance(_settings);
				var records = await logService.GetLogRecords(options);

				return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
		[Route("/ogle/Metrics")]
        public IActionResult Metrics(DateTime? date, LogReaderOptions options, bool autoFetchData, bool canDrillDown = true)
		{
            try
            {
                date ??= DateTime.Today.AddDays(-1);

                options.Date ??= DateOnly.FromDateTime(date.Value);
                options.MinutesPerBucket ??= _settings.CurrentValue.DefaultMinutesPerBucket;
                options.NumberOfBuckets ??= _settings.CurrentValue.DefaultNumberOfBuckets;

				var keyProps = _settings.CurrentValue.GroupKeyType.GetProperties()
										.Select(i => i.Name)
										.ToArray();
				var keyDisplayNames = _settings.CurrentValue.GroupKeyType.GetProperties()
											   .Select(i => (i.GetCustomAttribute(typeof(DisplayNameAttribute))
															 as DisplayNameAttribute)?.DisplayName ?? i.Name)
											   .ToArray();
				var valueProps = _settings.CurrentValue.MetricsType.GetProperties()
										  .Where(i => !keyProps.Contains(i.Name))
										  .Select(i => i.Name)
                                          .ToArray();
				var valueTypes = _settings.CurrentValue.MetricsType.GetProperties()
                                          .Where(i => !keyProps.Contains(i.Name))
                                          .Select(i => i.PropertyType.Name)
                                          .ToArray();
				var valueDisplayNames = _settings.CurrentValue.MetricsType.GetProperties()
												 .Where(i => !keyProps.Contains(i.Name))
												 .Select(i => (i.GetCustomAttribute(typeof(DisplayNameAttribute))
															   as DisplayNameAttribute)?.DisplayName ?? i.Name)
												 .ToArray();
				var aggregationFunc = _settings.CurrentValue.MetricsType.GetProperties()
											   .Where(i => !keyProps.Contains(i.Name))
											   .Select(i => i.GetCustomAttribute(typeof(AggregateAttribute)) as AggregateAttribute)
											   .Select(i => i != null ? i.AggregationOperation.ToString().LowerCaseFirstCharacter() : "sum")
											   .ToArray();
				var timeBuckets = GenerateTimeBuckets(options);
				var timeBucketProp = _settings.CurrentValue.MetricsType.GetProperties()
											  .Where(i => i.GetCustomAttributes(true).Any(j => j is TimeBucketAttribute))
											  .Select(i => i.Name.LowerCaseFirstCharacter())
											  .Single();
				var totalProp = _settings.CurrentValue.MetricsType.GetProperties()
                                         .Where(i => i.GetCustomAttributes(true).Any(j => j is TotalAttribute))
                                         .Select(i => i.Name.LowerCaseFirstCharacter())
                                         .SingleOrDefault() ?? valueProps.First();

                return View(new MetricsViewModel
				{
					RoutePrefix = GetRoutePrefix(),
					ServerUrls = _settings.CurrentValue.Hostnames.Select(i => GetHostnameUrl(Request.Scheme, i).ToString()).ToArray(),
					Date = options.Date.Value,
					HourFrom = options.HourFrom,
					MinuteFrom = options.MinuteFrom,
					MinutesPerBucket = options.MinutesPerBucket.Value,
					NumberOfBuckets = options.NumberOfBuckets.Value,
					DrillDownMinutesPerBucket = _settings.CurrentValue.DrillDownMinutesPerBucket,
					DrillDownNumberOfBuckets = _settings.CurrentValue.DrillDownNumberOfBuckets,
					AutoFetchData = autoFetchData,
					CanDrillDown = canDrillDown,
					KeyProperties = keyProps.Select(i => i.LowerCaseFirstCharacter()).ToArray(),
					KeyPropertyDisplayNames = keyDisplayNames,
					ValueProperties = valueProps.Select(i => i.LowerCaseFirstCharacter()).ToArray(),
					ValuePropertyTypes = valueTypes,
					ValuePropertyDisplayNames = valueDisplayNames,
					ValuePropertyAggregationOperation = aggregationFunc,
					TimeBuckets = timeBuckets,
					TimeBucketProperty = timeBucketProp,
					TotalProperty = totalProp
				});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetMetrics")]
        public async Task<IActionResult> GetMetrics(DateTime? date, LogReaderOptions options)
		{
            try
            {
                date ??= DateTime.Today.AddDays(-1);

                options.Date ??= DateOnly.FromDateTime(date.Value);
                options.MinutesPerBucket ??= _settings.CurrentValue.DefaultMinutesPerBucket;
                options.NumberOfBuckets ??= _settings.CurrentValue.DefaultNumberOfBuckets;

				var repo = ResolveLogMetricsRepository();
				var logService = LogServiceFactory.CreateInstance(_settings, repo);
				var metrics = await logService.GetLogMetrics(options, _settings.CurrentValue.GroupFunction);

				return Json(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetMetricsFromAllServers")]
        public async Task<IActionResult> GetMetricsFromAllServers(DateTime? date, LogReaderOptions options)
		{
            try
            {
                date ??= DateTime.Today.AddDays(-1);

                options.Date ??= DateOnly.FromDateTime(date.Value);
                options.MinutesPerBucket ??= _settings.CurrentValue.DefaultMinutesPerBucket;
                options.NumberOfBuckets ??= _settings.CurrentValue.DefaultNumberOfBuckets;

				var repo = ResolveLogMetricsRepository();

				if (repo != null)
				{
					var logService = LogServiceFactory.CreateInstance(_settings, repo);

					if (await logService.HasLogMetrics(options.Date.Value))
					{
						var metrics = await logService.GetLogMetrics(options, _settings.CurrentValue.GroupFunction);

						return Json(metrics);
					}
				}

				var endpoint = $"/{GetRoutePrefix()}/GetMetrics?date={options.Date.Value.ToString("yyyy-MM-dd")}&hourFrom={options.HourFrom}&minuteFrom={options.MinuteFrom}&minutesPerBucket={options.MinutesPerBucket.Value}&numberOfBuckets={options.NumberOfBuckets.Value}";
                var responses = await CollateJsonResponsesFromServers(endpoint);

                if (responses.All(i => i.Value.Error != null))
                {
                    return Problem(string.Join("\n", responses.Values.Select(i => i.Error)));
                }
                else
                {
                    var metrics = responses.Where(i => i.Value.Error == null)
                                           .SelectMany(i => i.Value.Payload)
                                           .ToArray();

                    return Json(metrics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/SaveMetricsFromAllServers")]
        public async Task<IActionResult> SaveMetricsFromAllServers(DateTime? date)
		{
			try
			{
				date ??= DateTime.Today.AddDays(-1);

				var dateOnly = DateOnly.FromDateTime(date.Value);
				var repo = ResolveLogMetricsRepository();
				var logService = LogServiceFactory.CreateInstance(_settings, repo);

				if (await logService.HasLogMetrics(dateOnly))
				{
					await logService.DeleteLogMetrics(dateOnly);
				}

                //1st save metrics using default groupping
                var numberOfBuckets = _settings.CurrentValue.DefaultNumberOfBuckets;
                var endpoint = $"/{GetRoutePrefix()}/GetMetrics?date={date.Value.ToString("yyyy-MM-dd")}&minutesPerBucket={_settings.CurrentValue.DefaultMinutesPerBucket}&numberOfBuckets={numberOfBuckets}";
                var responses = await CollateJsonResponsesFromServers(endpoint);

                if (responses.All(i => i.Value.Error != null))
                {
                    return Problem(string.Join("\n", responses.Values.Select(i => i.Error)));
                }
                else
                {
                    var rowsSaved = 0L;
                    var metrics = responses.Where(i => i.Value.Error == null)
                                       .SelectMany(i => i.Value.Payload)
                                       .ToArray();

                    //2nd save metrics using detailed groupping
                    numberOfBuckets = _settings.CurrentValue.DefaultNumberOfBuckets * _settings.CurrentValue.DrillDownNumberOfBuckets;
                    endpoint = $"/{GetRoutePrefix()}/GetMetrics?date={date.Value.ToString("yyyy-MM-dd")}&minutesPerBucket={_settings.CurrentValue.DrillDownMinutesPerBucket}&numberOfBuckets={numberOfBuckets}";
                    responses = await CollateJsonResponsesFromServers(endpoint);

                    if (responses.All(i => i.Value.Error != null))
                    {
                        return Problem(string.Join("\n", responses.Values.Select(i => i.Error)));
                    }
                    else
                    {
                        var detailedRowsSaved = 0L;
                        var detailedMetrics = responses.Where(i => i.Value.Error == null)
                                                       .SelectMany(i => i.Value.Payload)
                                                       .ToArray();

                        if (Enumerable.Any(metrics))
						{
							rowsSaved = await logService.SaveLogMetrics(dateOnly, ConvertToMetricsType(metrics), false);
						}
                        if (Enumerable.Any(detailedMetrics))
                        {
                            detailedRowsSaved = await logService.SaveLogMetrics(dateOnly, ConvertToMetricsType(detailedMetrics), true);
                        }

                        return Ok(new[] { rowsSaved, detailedRowsSaved });
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				throw;
			}
        }

        #endregion

        #region Private methods

		private string GetRoutePrefix()
		{
			var route = ControllerContext.ActionDescriptor.AttributeRouteInfo.Template;
			var slashIndex = route.LastIndexOf('/');

			if (slashIndex >= 0)
			{
				return route.Substring(0, slashIndex);
			}

			return route;
        }

        private static string[] GenerateTimeBuckets(LogReaderOptions options)
		{
			var t = new TimeOnly(options.HourFrom, options.MinuteFrom);
			var buckets = Enumerable.Range(0, options.NumberOfBuckets.Value)
									.Select(i => $"\"{t.AddMinutes(i * options.MinutesPerBucket.Value).ToString("HH:mm")}\"")
									.ToArray();

			return buckets;
		}

		private object? ResolveLogMetricsRepository()
		{
			var func = GetType().GetMethod("ResolveLogMetricsRepositoryImpl", BindingFlags.Instance | BindingFlags.NonPublic)
								.MakeGenericMethod(_settings.CurrentValue.MetricsType);
			var repo = func.Invoke(this, new object[0]);

			return repo;
		}

		private ILogMetricsRepository<T> ResolveLogMetricsRepositoryImpl<T>()
		{
			return _serviceProvider.GetService<ILogMetricsRepository<T>>();
		}

		private dynamic ConvertToMetricsType(object response)
		{
            var func = GetType().GetMethod("ConvertResponseToMetricsTypeImpl", BindingFlags.Instance | BindingFlags.NonPublic)
                                .MakeGenericMethod(_settings.CurrentValue.MetricsType);
            var converted = func.Invoke(this, new object[] { response });

            return converted;
        }

        private IEnumerable<T> ConvertResponseToMetricsTypeImpl<T>(IEnumerable<object> response)
        {
			return response.Cast<T>();
		}

		private UriBuilder GetHostnameUrl(string scheme, string hostname, string path = null)
		{
			var infoUrlBuilder = new UriBuilder(scheme, hostname);
			var httpsPort = _settings.CurrentValue.HttpsPort > 0 ? _settings.CurrentValue.HttpsPort : -1;
            var httpPort = _settings.CurrentValue.HttpPort > 0 ? _settings.CurrentValue.HttpPort : -1;

            infoUrlBuilder.Port = scheme == "https" ? httpsPort : httpPort;

			if (!string.IsNullOrEmpty(path))
			{
				var queryStringIndex = path.IndexOf('?');

				if (queryStringIndex >= 0)
				{
					infoUrlBuilder.Path = path[..queryStringIndex];
					infoUrlBuilder.Query = path[(queryStringIndex + 1)..];
				}
				else
				{
					infoUrlBuilder.Path = path;
				}
			}

			return infoUrlBuilder;
		}

        private async Task<Dictionary<string, NodeResponse<IEnumerable<object>>>> CollateJsonResponsesFromServers(string urlPath)
		{
			var hostnames = _settings.CurrentValue.Hostnames;
			var result = new ConcurrentBag<(string, NodeResponse<IEnumerable<object>>)>();
			var tasks = hostnames.Select(async hostname =>
			{
				UriBuilder infoUrlBuilder = GetHostnameUrl(Request.Scheme, hostname, urlPath);
				var actualServerUrl = $"{infoUrlBuilder.Scheme}://{infoUrlBuilder.Host}:{infoUrlBuilder.Port}";
				var infoUrl = infoUrlBuilder.ToString();

				using var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true })
				{
                    Timeout = _settings.CurrentValue.LogParserTimeout
                };

				string responseContent;
                IEnumerable<object> serverResponse;
				NodeResponse<IEnumerable<object>> responseWrapper;
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				try
				{
					var response = await client.GetAsync(infoUrl);
					var type = typeof(IEnumerable<>).MakeGenericType(new[] { _settings.CurrentValue.MetricsType });

                    responseContent = await response.Content.ReadAsStringAsync();
					options.Converters.Add(new JsonTimeSpanConverter());
                    //serverResponse = ConvertResponseToMetricsType(JsonSerializer.Deserialize(responseContent, type, options));
                    serverResponse = (IEnumerable<object>)JsonSerializer.Deserialize(responseContent, type, options);
                    //serverResponse = JsonSerializer.Deserialize<IEnumerable<object>>(responseContent, options);
                    responseWrapper = new NodeResponse<IEnumerable<object>>
					{
						Payload = serverResponse
					};
				}
				catch(Exception ex)
				{
                    var hre = ex as HttpRequestException;

                    responseWrapper = new NodeResponse<IEnumerable<object>>
                    {
                        Error = $"Endpoint {actualServerUrl} returned an error {hre?.StatusCode?.ToString() ?? string.Empty}: {ex.Message}"
                    };
                    _logger.LogError(ex, "{url} endpoint at {actualServerUrl} returned an error.\n{exceptionMessage}",
									 urlPath, actualServerUrl, ex.Message);
				}

				result.Add((actualServerUrl, responseWrapper));
			});

			await Task.WhenAll(tasks);

			return result.ToDictionary(k => k.Item1, v => v.Item2);
		}

		#endregion
	}
}

