using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using MathNet.Numerics.Statistics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Ae.Dns.Console
{
    public sealed class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            MeterListener meterListener = new()
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name.StartsWith("Ae.Dns"))
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };
            meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
            meterListener.Start();

            app.Run(async context =>
            {
                var responseBufferingFeature = context.Features.Get<IHttpResponseBodyFeature>();
                responseBufferingFeature?.DisableBuffering();

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/plain; charset=utf-8";

                async Task WriteString(string str)
                {
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(str), context.RequestAborted);
                    await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
                }

                var statsSets = new Dictionary<string, IDictionary<string, int>>
                {
                    { "Statistics", _stats },
                    { "Top Blocked Domains", _topBlockedDomains },
                    { "Top Permitted Domains", _topPermittedDomains },
                    { "Top Prefetched Domains", _topPrefetchedDomains },
                    { "Top Missing Domains", _topMissingDomains },
                    { "Top Other Error Domains", _topOtherErrorDomains },
                    { "Top Exception Error Domains", _topExceptionDomains },
                    { "Top Query Types", _topQueryTypes },
                    { "Top Record Types", _topRecordTypes }
                };

                async Task WriteResponseStatistics(string title, IReadOnlyCollection<float> values)
                {
                    await WriteString(title);
                    await WriteString(Environment.NewLine);
                    await WriteString(new string('=', title.Length));
                    await WriteString(Environment.NewLine);

                    if (values.Count > 0)
                    {
                        await WriteString($"avg = {values.Average():n2}s, sdv = {values.StandardDeviation():n2}s");
                        await WriteString(Environment.NewLine);
                        await WriteString($"p99 = {values.Percentile(99):n2}s, p90 = {values.Percentile(90):n2}s, p75 = {values.Percentile(75):n2}s");
                    }
                    else
                    {
                        await WriteString("None");
                    }

                    await WriteString(Environment.NewLine);
                    await WriteString(Environment.NewLine);
                }

                await WriteResponseStatistics($"Query Times ({_responseTimes.Count})", _responseTimes);
                await WriteResponseStatistics($"Record TTLs ({_responseTimes.Count})", _ttlTimes);

                foreach (var statsSet in statsSets)
                {
                    await WriteString(statsSet.Key);
                    await WriteString(Environment.NewLine);
                    await WriteString(new string('=', statsSet.Key.Length));
                    await WriteString(Environment.NewLine);

                    if (!statsSet.Value.Any())
                    {
                        await WriteString("None");
                        await WriteString(Environment.NewLine);
                    }

                    var limit = 25;
                    if (context.Request.Query.TryGetValue("limit", out var limitValues))
                    {
                        _ = int.TryParse(limitValues.ToString(), out limit);
                    }

                    foreach (var statistic in statsSet.Key == "Statistics" ? statsSet.Value.OrderBy(x => x.Key) : statsSet.Value.OrderByDescending(x => x.Value).Take(limit))
                    {
                        await WriteString($"{statistic.Key} = {statistic.Value}");
                        await WriteString(Environment.NewLine);
                    }

                    await WriteString(Environment.NewLine);
                }
            });
        }

        private readonly BlockingCollection<float> _responseTimes = new(sizeof(float) * 10_000_000);
        private readonly BlockingCollection<float> _ttlTimes = new(sizeof(float) * 10_000_000);
        private readonly ConcurrentDictionary<string, int> _stats = new();
        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topPrefetchedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topMissingDomains = new();
        private readonly ConcurrentDictionary<string, int> _topOtherErrorDomains = new();
        private readonly ConcurrentDictionary<string, int> _topExceptionDomains = new();
        private readonly ConcurrentDictionary<string, int> _topQueryTypes = new();
        private readonly ConcurrentDictionary<string, int> _topRecordTypes = new();

        private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
        {
            var metricId = $"{instrument.Meter.Name}.{instrument.Name}";

            _stats.AddOrUpdate(metricId, 1, (id, count) => count + 1);

            static TObject GetObjectFromTags<TObject>(ReadOnlySpan<KeyValuePair<string, object>> _tags, string name)
            {
                foreach (var tag in _tags)
                {
                    if (tag.Key == name)
                    {
                        return (TObject)tag.Value;
                    }
                }

                throw new InvalidOperationException();
            }

            if (instrument.Meter.Name == DnsMetricsClient.MeterName)
            {
                var meterMap = new Dictionary<string, ConcurrentDictionary<string, int>>
                {
                    { DnsMetricsClient.SuccessCounterName, _topPermittedDomains },
                    { DnsMetricsClient.OtherErrorCounterName, _topOtherErrorDomains },
                    { DnsMetricsClient.MissingErrorCounterName, _topMissingDomains },
                    { DnsMetricsClient.RefusedErrorCounterName, _topBlockedDomains },
                    { DnsMetricsClient.ExceptionErrorCounterName, _topExceptionDomains }
                };

                if (meterMap.TryGetValue(instrument.Name, out var domainCounts))
                {
                    var query = GetObjectFromTags<DnsMessage>(tags, "Query");

                    if (query.Header.Tags.ContainsKey("IsPrefetch"))
                    {
                        _topPrefetchedDomains.AddOrUpdate(GetObjectFromTags<DnsMessage>(tags, "Query").Header.Host, 1, (id, count) => count + 1);
                    }

                    domainCounts.AddOrUpdate(query.Header.Host, 1, (id, count) => count + 1);

                    if (instrument.Name == DnsMetricsClient.SuccessCounterName)
                    {
                        _topQueryTypes.AddOrUpdate(query.Header.QueryType.ToString(), 1, (id, count) => count + 1);

                        var answer = GetObjectFromTags<DnsMessage>(tags, "Answer");
                        if (!answer.Header.Tags.ContainsKey("IsCached"))
                        {
                            foreach (var record in answer.Answers)
                            {
                                _topRecordTypes.AddOrUpdate(record.Type.ToString(), 1, (id, count) => count + 1);
                                _ttlTimes.Add(record.TimeToLive);
                            }

                            _responseTimes.Add((float)GetObjectFromTags<Stopwatch>(tags, "Stopwatch").Elapsed.TotalSeconds);
                        }
                    }
                }
            }
        }
    }
}
