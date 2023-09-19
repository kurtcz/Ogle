using System;
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
using Ogle.Extensions;

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
        public IActionResult Index(string? id, string? hostName, DateTime? date, bool highlight = true)
        {
            try
            {
                var errorMessages = ValidateLogsConfiguration();

                if (errorMessages.Any())
                {
                    return View("Error", new ErrorViewModel
                    {
                        Layout = _settings.CurrentValue.Layout,
                        ErrorMessages = errorMessages.ToArray()
                    });
                }
                date ??= DateTime.Today.AddDays(-1);

                var model = new LogsViewModel
                {
                    Layout = _settings.CurrentValue.Layout,
                    RoutePrefix = ControllerContext.GetRoutePrefix(),
                    Id = id,
                    Date = DateOnly.FromDateTime(date.Value),
                    HostName = hostName,
                    Highlight = highlight,
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
        public async Task<IActionResult> BrowseLogFiles(DateTime? date, string? hostname)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hostname))
                {
                    var endpoint = $"/{ControllerContext.GetRoutePrefix()}/BrowseLogFiles?date={date:yyyy-MM-dd}";
                    var responses = await CollateJsonResponsesFromServers<string>(hostname, endpoint);

                    if (responses.All(i => !i.Value.StatusCode.IsSuccessCode()))
                    {
                        var result = string.Join("\n", responses.Values.Select(i => i.Error).Distinct());

                        if (string.IsNullOrWhiteSpace(result))
                        {
                            result = $"Browse failed for {date:yyyy-MM-dd}";
                        }

                        //unexpected error uses a text/plain mime type and a 500 status code
                        return Problem(result);
                    }
                    else
                    {
                        var result = string.Join("\n", responses.Where(i => !string.IsNullOrWhiteSpace(i.Value.Payload))
                                                                .Select(i => i.Value.Payload));

                        //search result from servers uses a text/html mime type
                        return Content(result, "text/html");
                    }
                }
                else
                {
                    dynamic logService = LogServiceFactory.CreateInstance(_settings);

                    var sb = new StringBuilder();
                    var fileNames = new StringBuilder();
                    var fileDates = new StringBuilder();
                    var fileSizes = new StringBuilder();
                    DateOnly? dateOnly = date.HasValue ? DateOnly.FromDateTime(date.Value) : null;

                    sb.AppendLine("<div class=\"browseFilesContainer\">");
                    fileNames.AppendLine("<pre>");
                    fileDates.AppendLine("<pre>");
                    fileSizes.AppendLine("<pre>");
                    fileNames.AppendLine("Filename");
                    fileDates.AppendLine("Last write time");
                    fileSizes.AppendLine("File size");
                    foreach (var path in logService.GetLogFilenames(dateOnly))
                    {
                        var fi = new FileInfo(path);
                        var filename = Path.GetFileName(path);
                        var url = Url.Action("DownloadLog", "ogle", new { log = filename }, Request.Scheme, Request.Host.ToUriComponent());

                        fileNames.AppendLine($"<a href=\"{url}\">{filename}</a>");
                        fileDates.AppendLine(fi.LastWriteTime.ToString());
                        fileSizes.AppendLine($"{fi.Length:N0} bytes");
                    }
                    fileNames.AppendLine("</pre>");
                    fileDates.AppendLine("</pre>");
                    fileSizes.AppendLine("</pre>");
                    sb.AppendLine(fileNames.ToString());
                    sb.AppendLine(fileDates.ToString());
                    sb.AppendLine(fileSizes.ToString());
                    sb.AppendLine("</div>\n");

                    return Content(sb.ToString(), "text/html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetLogs")]
        public async Task<IActionResult> GetLogs(DateTime? date, string id, bool highlight = true)
        {
            try
            {
                date ??= DateTime.Today.AddDays(-1);

                var searchPatternMatch = new Regex(_settings.CurrentValue.AllowedSearchPattern).Match(id);
                string result;

                if (id == null)
                {
                    return BadRequest("Please specify a request id or a unique search term");
                }
                else if (!searchPatternMatch.Success)
                {
                    return BadRequest($"Search term has to match {_settings.CurrentValue.AllowedSearchPattern} regular expresion pattern");
                }
                else
                {
                    dynamic logService = LogServiceFactory.CreateInstance(_settings);

                    result = await (Task<string>)logService.GetLogContent(id, DateOnly.FromDateTime(date.Value));

                    if (string.IsNullOrEmpty(result))
                    {
                        return NoContent();
                    }
                    else if (highlight)
                    {
                        result = (string)logService.HighlightLogContent(result, id);
                    }
                }

                return Content(result, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        [HttpGet]
        [Route("/ogle/GetLogsFromAllServers")]
        public async Task<IActionResult> GetLogsFromAllServers(DateTime? date, string? hostname, string id, bool? highlight = true)
        {
            try
            {
                date ??= DateTime.Today.AddDays(-1);

                var searchPatternMatch = new Regex(_settings.CurrentValue.AllowedSearchPattern).Match(id);
                string result;

                if (id == null)
                {
                    result = "Please specify a request id or a unique search term";
                }
                else if (!searchPatternMatch.Success)
                {
                    result = $"Search term has to match {_settings.CurrentValue.AllowedSearchPattern} regular expresion pattern";
                }
                else
                {
                    var endpoint = $"/{ControllerContext.GetRoutePrefix()}/GetLogs?date={date.Value.ToString("yyyy-MM-dd")}&id={id}&highlight={highlight}";
                    var responses = await CollateJsonResponsesFromServers<string>(hostname, endpoint);

                    if (responses.All(i => !i.Value.StatusCode.IsSuccessCode()))
                    {
                        result = string.Join("\n", responses.Values.Select(i => i.Error).Distinct());

                        if (string.IsNullOrWhiteSpace(result))
                        {
                            result = $"Search failed for {date:yyyy-MM-dd}";
                        }

                        //unexpected error uses a text/plain mime type and a 500 status code
                        return Problem(result);
                    }
                    else
                    {
                        result = string.Join("\n", responses.Where(i => !string.IsNullOrWhiteSpace(i.Value.Payload))
                                                            .Select(i => i.Value.Payload));
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            result = $"Request id or search term not found in logs for {date:yyyy-MM-dd}";
                        }
                        else
                        {
                            //search result from servers uses a text/html mime type
                            return Content($"<pre>{result}</pre>", "text/html");
                        }
                    }
                }

                //error message uses a text/plain mime type
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
                var errorMessages = ValidateMetricsConfiguration();

                if (errorMessages.Any())
                {
                    return View("Error", new ErrorViewModel
                    {
                        Layout = _settings.CurrentValue.Layout,
                        ErrorMessages = errorMessages.ToArray()
                    });
                }
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
                    Layout = _settings.CurrentValue.Layout,
                    RoutePrefix = ControllerContext.GetRoutePrefix(),
                    ServerUrls = _settings.CurrentValue.Hostnames.Select(i => GetHostnameUrl(Request.Scheme, i).ToString()).ToArray(),
                    Date = options.Date.Value,
                    HourFrom = options.HourFrom,
                    MinuteFrom = options.MinuteFrom,
                    MinutesPerBucket = options.MinutesPerBucket.Value,
                    NumberOfBuckets = options.NumberOfBuckets.Value,
                    DrillDownMinutesPerBucket = _settings.CurrentValue.DrillDownMinutesPerBucket,
                    DrillDownNumberOfBuckets = _settings.CurrentValue.DrillDownNumberOfBuckets,
                    ViewButtonsPosition = _settings.CurrentValue.MetricsButtonsPosition,
                    FilterPosition = _settings.CurrentValue.FilterControlsPosition,
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
                var detail = options.MinutesPerBucket == _settings.CurrentValue.DrillDownMinutesPerBucket;

                if (repo != null)
                {
                    var logService = LogServiceFactory.CreateInstance(_settings, repo);

                    if (await logService.HasLogMetrics(options.Date.Value, detail))
                    {
                        var metrics = await logService.GetLogMetrics(options, _settings.CurrentValue.GroupFunction);

                        return Json(metrics);
                    }
                }

                var endpoint = $"/{ControllerContext.GetRoutePrefix()}/GetMetrics?date={options.Date.Value.ToString("yyyy-MM-dd")}&hourFrom={options.HourFrom}&minuteFrom={options.MinuteFrom}&minutesPerBucket={options.MinutesPerBucket.Value}&numberOfBuckets={options.NumberOfBuckets.Value}";
                var responses = await CollateJsonResponsesFromServers<IEnumerable<object>>(endpoint);

                if (responses.All(i => !i.Value.StatusCode.IsSuccessCode()))
                {
                    return Problem(string.Join("\n", responses.Values.Select(i => i.Error).Distinct()));
                }
                else
                {
                    var metrics = responses.Where(i => i.Value.Payload != null)
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

                if (await logService.HasLogMetrics(dateOnly, false))
                {
                    await logService.DeleteLogMetrics(dateOnly, false);
                }
                if (await logService.HasLogMetrics(dateOnly, true))
                {
                    await logService.DeleteLogMetrics(dateOnly, true);
                }

                //1st save metrics using default groupping
                var numberOfBuckets = _settings.CurrentValue.DefaultNumberOfBuckets;
                var endpoint = $"/{ControllerContext.GetRoutePrefix()}/GetMetrics?date={date.Value.ToString("yyyy-MM-dd")}&minutesPerBucket={_settings.CurrentValue.DefaultMinutesPerBucket}&numberOfBuckets={numberOfBuckets}";
                var responses = await CollateJsonResponsesFromServers<IEnumerable<object>>(endpoint);

                if (responses.All(i => !i.Value.StatusCode.IsSuccessCode()))
                {
                    return Problem(string.Join("\n", responses.Values.Select(i => i.Error).Distinct()));
                }
                else
                {
                    var rowsSaved = 0L;
                    var metrics = responses.Where(i => i.Value.Payload != null)
                                           .SelectMany(i => i.Value.Payload)
                                           .ToArray();

                    //2nd save metrics using detailed groupping
                    numberOfBuckets = _settings.CurrentValue.DefaultNumberOfBuckets * _settings.CurrentValue.DrillDownNumberOfBuckets;
                    endpoint = $"/{ControllerContext.GetRoutePrefix()}/GetMetrics?date={date.Value.ToString("yyyy-MM-dd")}&minutesPerBucket={_settings.CurrentValue.DrillDownMinutesPerBucket}&numberOfBuckets={numberOfBuckets}";
                    responses = await CollateJsonResponsesFromServers<IEnumerable<object>>(endpoint);

                    if (responses.All(i => !i.Value.StatusCode.IsSuccessCode()))
                    {
                        return Problem(string.Join("\n", responses.Values.Select(i => i.Error).Distinct()));
                    }
                    else
                    {
                        var detailedRowsSaved = 0L;
                        var detailedMetrics = responses.Where(i => i.Value.Payload != null)
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

        [HttpGet]
        [Route("/ogle/config")]
        public IActionResult Configuration()
        {
            dynamic repo = ResolveLogMetricsRepository();
            var repoName = repo == null ? "not set" : repo.GetType().FullName.Split('`')[0];
            var repoSettings = (Dictionary<string, string>?)repo?.GetConfiguration();

            var model = new ConfigurationViewModel
            {
                Layout = _settings.CurrentValue.Layout,
                OgleRegistration = new Dictionary<string, string>
                {
                    { "Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "null" },
                    { "UseOgleRoutePrefix", $"\"{ControllerContext.GetRoutePrefix()}\"" },
                    { "Repository", repoName }
                },
                OgleOptions = _settings.CurrentValue.ToPropertyDictionary(),
                OgleRepositoryOptions = repoSettings
            };

            return View(model);
        }

        #endregion

        #region Private methods

        private IEnumerable<string> ValidateLogsConfiguration()
        {
            var result = new List<string>();

            if (_settings.CurrentValue.GroupKeyType == null)
            {
                result.Add($"OgleOptions.GroupKeyType is not defined");
            }
            if (_settings.CurrentValue.RecordType == null)
            {
                result.Add($"OgleOptions.RecordType is not defined");
            }
            if (!_settings.CurrentValue.RecordType.IsSubclassOf(_settings.CurrentValue.GroupKeyType))
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} must be a subclass of {_settings.CurrentValue.GroupKeyType.FullName}");
            }
            if (!_settings.CurrentValue.MetricsType.IsSubclassOf(_settings.CurrentValue.GroupKeyType))
            {
                result.Add($"{_settings.CurrentValue.MetricsType.FullName} must be a subclass of {_settings.CurrentValue.GroupKeyType.FullName}");
            }

            var keyProps = _settings.CurrentValue.GroupKeyType.GetProperties()
                                    .Select(i => i.Name)
                                    .ToArray();

            if (!keyProps.Any())
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class does not define any properties");
            }

            var mandatoryLogPatternProps = _settings.CurrentValue.GroupKeyType.GetCustomAttributes(true)
                                                    .Where(i => i is MandatoryLogPatternAttribute)
                                                    .ToList();

            if (!mandatoryLogPatternProps.Any())
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class is not decorated with [MandatoryLogPattern] attribute");
            }

            var mandatoryKeyProp = _settings.CurrentValue.RecordType.GetProperties()
                                            .Where(i => i.GetCustomAttributes(true)
                                                         .Any(j => (j as MandatoryAttribute)?.IsKey ?? false))
                                            .ToList();
            if (!mandatoryKeyProp.Any())
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class does not define a property decorated with [Mandatory(isKey: true)] attribute");
            }
            if (mandatoryKeyProp.Count > 1)
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class can only define a single property decorated with [Mandatory(isKey: true)] attribute");
            }

            return result;
        }

        private IEnumerable<string> ValidateMetricsConfiguration()
        {
            var result = new List<string>();

            if (_settings.CurrentValue.GroupKeyType == null)
            {
                result.Add($"OgleOptions.GroupKeyType is not defined");
            }
            if (_settings.CurrentValue.RecordType == null)
            {
                result.Add($"OgleOptions.RecordType is not defined");
            }
            if (_settings.CurrentValue.MetricsType == null)
            {
                result.Add($"OgleOptions.MetricsType is not defined");
            }
            if (_settings.CurrentValue.GroupFunction == null)
            {
                result.Add($"OgleOptions.GroupFunction is not defined");
            }

            if (!_settings.CurrentValue.RecordType.IsSubclassOf(_settings.CurrentValue.GroupKeyType))
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} must be a subclass of {_settings.CurrentValue.GroupKeyType.FullName}");
            }
            if (!_settings.CurrentValue.MetricsType.IsSubclassOf(_settings.CurrentValue.GroupKeyType))
            {
                result.Add($"{_settings.CurrentValue.MetricsType.FullName} must be a subclass of {_settings.CurrentValue.GroupKeyType.FullName}");
            }

            var keyProps = _settings.CurrentValue.GroupKeyType.GetProperties()
                                    .Select(i => i.Name)
                                    .ToArray();

            if (!keyProps.Any())
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class does not define any properties");
            }

            var valueProps = _settings.CurrentValue.MetricsType.GetProperties()
                                      .Where(i => !keyProps.Contains(i.Name))
                                      .Select(i => i.Name)
                                      .ToArray();

            if (!valueProps.Any())
            {
                result.Add($"{_settings.CurrentValue.MetricsType.FullName} class does not define any properties");
            }

            var timeBucketProp = _settings.CurrentValue.GroupKeyType.GetProperties()
                                          .Where(i => i.GetCustomAttributes(true).Any(j => j is TimeBucketAttribute))
                                          .Select(i => i.Name.LowerCaseFirstCharacter())
                                          .ToList();

            if (!timeBucketProp.Any())
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class does not define a property decorated with [TimeBucket] attribute");
            }
            if (timeBucketProp.Count > 1)
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class can only define a single [TimeBucket] attribute");
            }

            var totalProp = _settings.CurrentValue.MetricsType.GetProperties()
                                     .Where(i => i.GetCustomAttributes(true).Any(j => j is TotalAttribute))
                                     .Select(i => i.Name.LowerCaseFirstCharacter())
                                     .ToList();

            if (!totalProp.Any())
            {
                result.Add($"{_settings.CurrentValue.MetricsType.FullName} class does not define a property decorated with [Total] attribute");
            }
            if (totalProp.Count > 1)
            {
                result.Add($"{_settings.CurrentValue.MetricsType.FullName} class can only define a single [Total] attribute");
            }

            var mandatoryProps = _settings.CurrentValue.RecordType.GetProperties()
                                          .Where(i => i.GetCustomAttributes(true).Any(j => j is MandatoryAttribute))
                                          .Select(i => i.Name.LowerCaseFirstCharacter())
                                          .ToList();

            if (!mandatoryProps.Any())
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class does not define a property decorated with [Mandatory] attribute");
            }

            var mandatoryLogPatternProps = _settings.CurrentValue.GroupKeyType.GetCustomAttributes(true)
                                                    .Where(i => i is MandatoryLogPatternAttribute)
                                                    .ToList();

            if (!mandatoryLogPatternProps.Any())
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} class is not decorated with [MandatoryLogPattern] attribute");
            }

            var mandatoryKeyProp = _settings.CurrentValue.RecordType.GetProperties()
                                            .Where(i => i.GetCustomAttributes(true)
                                                         .Any(j => (j as MandatoryAttribute)?.IsKey ?? false))
                                            .ToList();
            if (!mandatoryKeyProp.Any())
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class does not define a property decorated with [Mandatory(isKey: true)] attribute");
            }
            if (mandatoryKeyProp.Count > 1)
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class can only define a single property decorated with [Mandatory(isKey: true)] attribute");
            }

            var logPatternProps = _settings.CurrentValue.RecordType.GetProperties()
                                           .Where(i => i.GetCustomAttributes(true).Any(j => j is LogPatternAttribute))
                                           .Select(i => i.Name.LowerCaseFirstCharacter())
                                           .ToList();

            if (!logPatternProps.Any())
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName} class does not define a property decorated with [LogPattern] attribute");
            }

            var equalsInfo = _settings.CurrentValue.GroupKeyType.GetMethod("Equals", new Type[] { typeof(object) });

            if (equalsInfo == null || equalsInfo.DeclaringType != _settings.CurrentValue.GroupKeyType)
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} must override the Equals method for groupping of metrics to work");
            }

            var getHashCodeInfo = _settings.CurrentValue.GroupKeyType.GetMethod("GetHashCode", new Type[] { });

            if (getHashCodeInfo == null || getHashCodeInfo.DeclaringType != _settings.CurrentValue.GroupKeyType)
            {
                result.Add($"{_settings.CurrentValue.GroupKeyType.FullName} must override the GetHashCode method for groupping of metrics to work");
            }

            equalsInfo = _settings.CurrentValue.RecordType.GetMethod("Equals", new Type[] { typeof(object) });

            if (equalsInfo != null && equalsInfo.DeclaringType != _settings.CurrentValue.GroupKeyType)
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName}.Equals method needs to be declared in {_settings.CurrentValue.GroupKeyType.FullName} for groupping of metrics to work");
            }

            getHashCodeInfo = _settings.CurrentValue.RecordType.GetMethod("GetHashCode", new Type[] { });

            if (getHashCodeInfo != null && getHashCodeInfo.DeclaringType != _settings.CurrentValue.GroupKeyType)
            {
                result.Add($"{_settings.CurrentValue.RecordType.FullName}.GetHashCode method needs to be declared in {_settings.CurrentValue.GroupKeyType.FullName} for groupping of metrics to work");
            }

            if (_settings.CurrentValue.DefaultMinutesPerBucket < 1)
            {
                result.Add($"Value must be at least one: OgleOptions.DefaultMinutesPerBucket");
            }
            if (_settings.CurrentValue.DefaultNumberOfBuckets < 1)
            {
                result.Add($"Value must be at least one: OgleOptions.DefaultNumberOfBuckets");
            }
            if (_settings.CurrentValue.DrillDownMinutesPerBucket < 1)
            {
                result.Add($"Value must be at least one: OgleOptions.DrillDownMinutesPerBucket");
            }
            if (_settings.CurrentValue.DrillDownNumberOfBuckets < 1)
            {
                result.Add($"Value must be at least one: OgleOptions.DrillDownNumberOfBuckets");
            }
            if (_settings.CurrentValue.DefaultMinutesPerBucket <= _settings.CurrentValue.DrillDownMinutesPerBucket)
            {
                result.Add($"OgleOptions.DefaultMinutesPerBucket must be greater than OgleOptions.DrillDownMinutesPerBucket");
            }
            if (_settings.CurrentValue.DefaultMinutesPerBucket * _settings.CurrentValue.DefaultNumberOfBuckets != 1440)
            {
                result.Add($"OgleOptions.DefaultMinutesPerBucket times OgleOptions.DefaultNumberOfBuckets has to be equal to number minutes per day (1440)");
            }
            if (_settings.CurrentValue.DrillDownMinutesPerBucket * _settings.CurrentValue.DrillDownNumberOfBuckets != _settings.CurrentValue.DefaultMinutesPerBucket)
            {
                result.Add($"OgleOptions.DrillDownMinutesPerBucket times OgleOptions.DrillDownNumberOfBuckets has to be equal to OgleOptions.DefaultMinutesPerBucket ({_settings.CurrentValue.DefaultMinutesPerBucket})");
            }

            return result;
        }

        private IActionResult FormatErrorResponse(Exception ex, int? statusCode = null)
        {
            return Problem($"{ex.GetType()}: {ex.Message}\n{ex.StackTrace}", statusCode: statusCode, title: ex.Message, type: ex.GetType().ToString());
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

        private async Task<Dictionary<string, NodeResponse<TServerResponse>>> CollateJsonResponsesFromServers<TServerResponse>(string urlPath)
            where TServerResponse : class
        {
            return await CollateJsonResponsesFromServers<TServerResponse>(null, urlPath);
        }

        private async Task<Dictionary<string, NodeResponse<TServerResponse>>> CollateJsonResponsesFromServers<TServerResponse>(string? hostname, string urlPath)
            where TServerResponse : class
        {
            var hostnames = string.IsNullOrEmpty(hostname) ? _settings.CurrentValue.Hostnames : new[] { hostname };
            var result = new ConcurrentBag<(string, NodeResponse<TServerResponse>)>();
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
                TServerResponse serverResponse;
                NodeResponse<TServerResponse> responseWrapper;

                try
                {
                    var response = await client.GetAsync(infoUrl);
                    var type = typeof(IEnumerable<>).MakeGenericType(new[] { _settings.CurrentValue.MetricsType });

                    responseContent = await response.Content.ReadAsStringAsync();
                    if (!response.StatusCode.IsSuccessCode())
                    {
                        responseWrapper = new NodeResponse<TServerResponse>
                        {
                            StatusCode = response.StatusCode,
                            Error = $"Error {(int)response.StatusCode} received from {urlPath}\n{responseContent}"
                        };
                    }
                    else if (typeof(TServerResponse) == typeof(string))
                    {
                        responseWrapper = new NodeResponse<TServerResponse>
                        {
                            StatusCode = response.StatusCode,
                            Payload = responseContent as TServerResponse
                        };
                    }
                    else if (response.Content.Headers.ContentType?.MediaType == "application/json")
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                        options.Converters.Add(new JsonTimeSpanConverter());
                        serverResponse = (TServerResponse)JsonSerializer.Deserialize(responseContent, type, options);
                        responseWrapper = new NodeResponse<TServerResponse>
                        {
                            StatusCode = response.StatusCode,
                            Payload = serverResponse
                        };
                    }
                    else
                    {
                        var mime = response.Content.Headers.ContentType?.MediaType ?? "(null)";
                        throw new InvalidDataException($"Unexpected response type {mime}");
                    }
                }
                catch (Exception ex)
                {
                    var hre = ex as HttpRequestException;

                    responseWrapper = new NodeResponse<TServerResponse>
                    {
                        Error = $"Error {hre?.StatusCode?.ToString() ?? string.Empty} returned from {actualServerUrl}{urlPath}\n{ex.GetType()}: {ex.Message}"
                    };
                    _logger.LogError(ex, "Error {statusCode} returned from {actualServerUrl}{urlPath}\n{exceptionType}: {exceptionMessage}",
                                     hre?.StatusCode?.ToString() ?? string.Empty,
                                     actualServerUrl,
                                     urlPath,
                                     ex.GetType(),
                                     ex.Message);
                }

                result.Add((actualServerUrl, responseWrapper));
            });

            await Task.WhenAll(tasks);

            return result.ToDictionary(k => k.Item1, v => v.Item2);
        }

        #endregion
    }
}

