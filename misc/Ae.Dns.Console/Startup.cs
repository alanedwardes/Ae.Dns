using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            MeterListener meterListener = new MeterListener
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

                if (context.Request.Path.StartsWithSegments("/live"))
                {
                    await WriteString("Listening..." + Environment.NewLine);

                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        _lastListenTime = DateTimeOffset.UtcNow;

                        if (_queries.TryDequeue(out var query))
                        {
                            await WriteString(query + Environment.NewLine);
                        }

                        if (_answers.TryDequeue(out var answer))
                        {
                            await WriteString(answer + Environment.NewLine);
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(10), context.RequestAborted);
                    }
                    return;
                }

                var statsSets = new Dictionary<string, IDictionary<string, int>>
                {
                    { "Statistics", _stats },
                    { "Top Blocked Domains", _topBlockedDomains },
                    { "Top Permitted Domains", _topPermittedDomains },
                    { "Top Prefetched Domains", _topPrefetchedDomains }
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

        private DateTimeOffset _lastListenTime;
        private readonly BlockingCollection<float> _responseTimes = new BlockingCollection<float>(sizeof(float) * 10_000_000);
        private readonly ConcurrentDictionary<string, int> _stats = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topPrefetchedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentQueue<DnsHeader> _queries = new ConcurrentQueue<DnsHeader>();
        private readonly ConcurrentQueue<DnsAnswer> _answers = new ConcurrentQueue<DnsAnswer>();

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

            if (metricId == "Ae.Dns.Client.DnsFilterClient.Blocked")
            {
                _topBlockedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            if (metricId == "Ae.Dns.Client.DnsFilterClient.Allowed")
            {
                _topPermittedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            if (metricId.StartsWith("Ae.Dns.Client.DnsCachingClient.") && metricId.EndsWith(".Prefetch"))
            {
                _topPrefetchedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            bool IsListening() => DateTimeOffset.UtcNow - _lastListenTime > TimeSpan.FromSeconds(5);

            if (metricId == "Ae.Dns.Server.DnsUdpServer.Query")
            {
                if (IsListening())
                {
                    _queries.Clear();
                }
                else
                {
                    _queries.Enqueue(GetObjectFromTags<DnsHeader>(tags, "Query"));
                }
            }

            if (metricId == "Ae.Dns.Server.DnsUdpServer.Response")
            {
                if (IsListening())
                {
                    _answers.Clear();
                }
                else
                {
                    _answers.Enqueue(GetObjectFromTags<DnsAnswer>(tags, "Answer"));
                }

                _responseTimes.Add((float)GetObjectFromTags<Stopwatch>(tags, "Stopwatch").Elapsed.TotalSeconds);
            }
        }
    }
}
