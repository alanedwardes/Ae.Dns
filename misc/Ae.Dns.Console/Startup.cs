using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Builder;

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
                var statsSets = new Dictionary<string, IDictionary<string, int>>
                {
                    { "Statistics", _stats },
                    { "Top Blocked Domains", _topBlockedDomains },
                    { "Top Permitted Domains", _topPermittedDomains },
                    { "Top Upstream Successes", _topUpstreamSuccesses },
                    { "Top Upstream Failures", _topUpstreamFailures }
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

        private readonly ConcurrentDictionary<string, int> _stats = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topUpstreamSuccesses = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _topUpstreamFailures = new ConcurrentDictionary<string, int>();

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

            if (metricId == "Ae.Dns.Client.DnsFilterClient.blocked")
            {
                _topBlockedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            if (metricId == "Ae.Dns.Client.DnsFilterClient.allowed")
            {
                _topPermittedDomains.AddOrUpdate(GetObjectFromTags<DnsHeader>(tags, "Query").Host, 1, (id, count) => count + 1);
            }

            if (metricId == "Ae.Dns.Client.DnsHttpClient.success")
            {
                _topUpstreamSuccesses.AddOrUpdate(GetObjectFromTags<Uri>(tags, "Address").Host, 1, (id, count) => count + 1);
            }

            if (metricId == "Ae.Dns.Client.DnsHttpClient.failure")
            {
                _topUpstreamFailures.AddOrUpdate(GetObjectFromTags<Uri>(tags, "Address").Host, 1, (id, count) => count + 1);
            }
        }
    }
}
