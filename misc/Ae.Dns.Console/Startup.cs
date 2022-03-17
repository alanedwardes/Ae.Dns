using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ae.Dns.Protocol;
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

            app.Use(async (context, next) =>
            {
                var responseBufferingFeature = context.Features.Get<IHttpResponseBodyFeature>();
                responseBufferingFeature?.DisableBuffering();

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/plain; charset=utf-8";

                if (context.Request.Path.StartsWithSegments("/live"))
                {
                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        _lastListenTime = DateTimeOffset.UtcNow;

                        if (_requests.TryDequeue(out var header))
                        {
                            await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(header + Environment.NewLine), context.RequestAborted);
                            await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(10), context.RequestAborted);
                    }
                    return;
                }

                var statsSets = new Dictionary<string, IDictionary<string, int>>
                {
                    { "Statistics", _stats },
                    { "Top Blocked Domains", _topBlockedDomains },
                    { "Top Permitted Domains", _topPermittedDomains }
                };

                foreach (var statsSet in statsSets)
                {
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(statsSet.Key));
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(new string('=', statsSet.Key.Length)));
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));

                    if (!statsSet.Value.Any())
                    {
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("None"));
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));
                    }

                    foreach (var statistic in statsSet.Value.OrderByDescending(x => x.Value))
                    {
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"{statistic.Key} = {statistic.Value}"));
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));
                    }

                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));
                }
            });
        }

        private DateTimeOffset _lastListenTime;
        private readonly ConcurrentDictionary<string, int> _stats = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentQueue<DnsHeader> _requests = new ConcurrentQueue<DnsHeader>();

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

            if (metricId == "Ae.Dns.Server.DnsUdpServer.Query")
            {
                if (DateTimeOffset.UtcNow - _lastListenTime > TimeSpan.FromSeconds(5))
                {
                    _requests.Clear();
                }
                else
                {
                    _requests.Enqueue(GetObjectFromTags<DnsHeader>(tags, "Query"));
                }
            }
        }
    }
}
