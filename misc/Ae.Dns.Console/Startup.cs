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
                    { "Top Other Error Domains", _topOtherErrorDomains }
                };

                if (_responseTimes.Count > 0)
                {
                    var responseTimesTitle = $"Query Response Statistics ({_responseTimes.Count})";
                    await WriteString(responseTimesTitle);
                    await WriteString(Environment.NewLine);
                    await WriteString(new string('=', responseTimesTitle.Length));
                    await WriteString(Environment.NewLine);
                    await WriteString($"avg = {_responseTimes.Average():n2}s");
                    await WriteString(Environment.NewLine);
                    await WriteString($"p90 = {_responseTimes.Percentile(90):n2}s");
                    await WriteString(Environment.NewLine);
                    await WriteString($"p99 = {_responseTimes.Percentile(99):n2}s");
                    await WriteString(Environment.NewLine);
                    await WriteString($"p75 = {_responseTimes.Percentile(75):n2}s");
                    await WriteString(Environment.NewLine);
                    await WriteString($"sdv = {_responseTimes.StandardDeviation():n2}s");
                    await WriteString(Environment.NewLine);
                    await WriteString(Environment.NewLine);
                }

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

                    foreach (var statistic in statsSet.Value.OrderByDescending(x => x.Value).Take(25))
                    {
                        await WriteString($"{statistic.Key} = {statistic.Value}");
                        await WriteString(Environment.NewLine);
                    }

                    await WriteString(Environment.NewLine);
                }
            });
        }

        private readonly ConcurrentDictionary<string, int> _topExceptionDomains = new();
        private readonly BlockingCollection<float> _responseTimes = new(sizeof(float) * 10_000_000);
        private readonly ConcurrentDictionary<string, int> _stats = new();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topPrefetchedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topMissingDomains = new();
        private readonly ConcurrentDictionary<string, int> _topOtherErrorDomains = new();
        private readonly ConcurrentQueue<DnsHeader> _queries = new();
        private readonly ConcurrentQueue<DnsAnswer> _answers = new();

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

            if (metricId.StartsWith("Ae.Dns.Client.DnsCachingClient.") && metricId.EndsWith(".Prefetch"))
            {
                _topPrefetchedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            if (metricId == "Ae.Dns.Server.DnsUdpServer.Query")
            {
                _queries.Enqueue(GetObjectFromTags<DnsHeader>(tags, "Query"));
            }

            if (metricId == "Ae.Dns.Server.DnsUdpServer.Response")
            {
                _answers.Enqueue(GetObjectFromTags<DnsAnswer>(tags, "Answer"));
                _responseTimes.Add((float)GetObjectFromTags<Stopwatch>(tags, "Stopwatch").Elapsed.TotalSeconds);
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
                    domainCounts.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
                }
            }
        }
    }
}
