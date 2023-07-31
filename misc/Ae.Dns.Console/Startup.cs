using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Server.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

            app.UseMiddleware<DnsMiddleware>();

            app.Run(async context =>
            {
                async Task WriteHeader(string header)
                {
                    await context.Response.WriteAsync(header);
                    await context.Response.WriteAsync(Environment.NewLine);
                    await context.Response.WriteAsync(new string('=', header.Length));
                    await context.Response.WriteAsync(Environment.NewLine);
                }

                var resolverCache = context.RequestServices.GetRequiredService<ObjectCache>();

                if (context.Request.Path.StartsWithSegments("/cache/remove"))
                {
                    var cacheKeyToRemove = context.Request.Path.Value.Split("/").Last();
                    resolverCache.Remove(cacheKeyToRemove);
                    context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                    context.Response.Headers.Location = "/cache";
                    return;
                }

                if (context.Request.Path.StartsWithSegments("/cache"))
                {
                    var cacheEntries = resolverCache.Where(x => x.Value is DnsCachingClient.DnsCacheEntry)
                        .Select(x => KeyValuePair.Create(x.Key, (DnsCachingClient.DnsCacheEntry)x.Value))
                        .OrderBy(x => x.Value.Expires)
                        .ToList();

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/html; charset=utf-8";

                    await context.Response.WriteAsync($"<h1>{resolverCache.Name}</h1>");
                    await context.Response.WriteAsync($"<p>Cached Objects = {cacheEntries.Count()}</p>");

                    await context.Response.WriteAsync("<table>");
                    await context.Response.WriteAsync("<thead>");
                    await context.Response.WriteAsync("<tr>");
                    await context.Response.WriteAsync("<th>Cache Key</th>");
                    await context.Response.WriteAsync("<th>Expires</th>");
                    await context.Response.WriteAsync("<th>Hits</th>");
                    await context.Response.WriteAsync("<th>Response Code</th>");
                    await context.Response.WriteAsync("<th>Actions</th>");
                    await context.Response.WriteAsync("</tr>");
                    await context.Response.WriteAsync("</thead>");
                    await context.Response.WriteAsync("<tbody>");

                    foreach (var entry in cacheEntries)
                    {
                        var message = new DnsMessage();
                        var offset = 0;
                        message.ReadBytes(entry.Value.Data, ref offset);

                        await context.Response.WriteAsync("<tr>");
                        await context.Response.WriteAsync($"<td>{entry.Key}</td>");
                        await context.Response.WriteAsync($"<td>{entry.Value.Expires.TotalSeconds:N0}s</td>");
                        await context.Response.WriteAsync($"<td>{entry.Value.Hits}</td>");
                        await context.Response.WriteAsync($"<td>{message.Header.ResponseCode}</td>");
                        await context.Response.WriteAsync($"<td><a href=\"/cache/remove/{entry.Key}\">🗑</a></td>");
                        await context.Response.WriteAsync("</tr>");
                    }

                    await context.Response.WriteAsync("</tbody>");
                    await context.Response.WriteAsync("</table>");
                }

                if (context.Request.Path == "/")
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/plain; charset=utf-8";

                    var statsSets = new Dictionary<string, IDictionary<string, int>>
                    {
                        { "Top Blocked Domains", _topBlockedDomains },
                        { "Top Permitted Domains", _topPermittedDomains },
                        { "Top Missing Domains", _topMissingDomains },
                        { "Top Other Error Domains", _topOtherErrorDomains },
                        { "Top Exception Error Domains", _topExceptionDomains }
                    };

                    await WriteHeader(resolverCache.Name);
                    await context.Response.WriteAsync($"Cached Objects = {resolverCache.Count()}");
                    await context.Response.WriteAsync(Environment.NewLine);
                    await context.Response.WriteAsync(Environment.NewLine);

                    foreach (var statsSet in statsSets)
                    {
                        await WriteHeader(statsSet.Key);

                        if (!statsSet.Value.Any())
                        {
                            await context.Response.WriteAsync("None");
                            await context.Response.WriteAsync(Environment.NewLine);
                        }

                        foreach (var statistic in statsSet.Key == "Statistics" ? statsSet.Value.OrderBy(x => x.Key) : statsSet.Value.OrderByDescending(x => x.Value).Take(50))
                        {
                            await context.Response.WriteAsync($"{statistic.Key} = {statistic.Value}");
                            await context.Response.WriteAsync(Environment.NewLine);
                        }

                        await context.Response.WriteAsync(Environment.NewLine);
                    }
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
                    var sourceEndpoint = query.Header.Tags.TryGetValue("Sender", out var rawEndpoint) && rawEndpoint is IPEndPoint endpoint ? endpoint : throw new Exception();

                    domainCounts.AddOrUpdate(sourceEndpoint.Address.ToString() + ' ' + query.Header.QueryType.ToString() + ' ' + query.Header.Host, 1, (id, count) => count + 1);
                }
            }
        }
    }
}
