using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ae.Dns.Client;
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
                    { "Top Blocked Domains", _topBlockedDomains },
                    { "Top Permitted Domains", _topPermittedDomains },
                    { "Top Missing Domains", _topMissingDomains },
                    { "Top Other Error Domains", _topOtherErrorDomains },
                    { "Top Exception Error Domains", _topExceptionDomains }
                };

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

                    foreach (var statistic in statsSet.Key == "Statistics" ? statsSet.Value.OrderBy(x => x.Key) : statsSet.Value.OrderByDescending(x => x.Value).Take(25))
                    {
                        await WriteString($"{statistic.Key} = {statistic.Value}");
                        await WriteString(Environment.NewLine);
                    }

                    await WriteString(Environment.NewLine);
                }
            });
        }

        private readonly ConcurrentDictionary<string, int> _topPermittedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topBlockedDomains = new();
        private readonly ConcurrentDictionary<string, int> _topMissingDomains = new();
        private readonly ConcurrentDictionary<string, int> _topOtherErrorDomains = new();
        private readonly ConcurrentDictionary<string, int> _topExceptionDomains = new();

        private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
        {
            var metricId = $"{instrument.Meter.Name}.{instrument.Name}";

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
                    domainCounts.AddOrUpdate(query.Header.QueryType.ToString() + ' ' + query.Header.Host, 1, (id, count) => count + 1);
                }
            }
        }
    }
}
