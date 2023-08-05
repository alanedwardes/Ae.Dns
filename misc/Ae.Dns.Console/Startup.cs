using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Server.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

                async Task WriteTable(DataTable table)
                {
                    await context.Response.WriteAsync("<table>");
                    await context.Response.WriteAsync("<thead>");
                    await context.Response.WriteAsync("<tr>");
                    foreach (DataColumn heading in table.Columns)
                    {
                        await context.Response.WriteAsync($"<th>{heading.ColumnName}</th>");
                    }
                    await context.Response.WriteAsync("</tr>");
                    await context.Response.WriteAsync("</thead>");

                    await context.Response.WriteAsync("<tbody>");
                    foreach (DataRow row in table.Rows)
                    {
                        await context.Response.WriteAsync("<tr>");
                        foreach (var item in row.ItemArray)
                        {
                            await context.Response.WriteAsync($"<td>{item}</td>");
                        }
                        await context.Response.WriteAsync("</tr>");
                    }
                    await context.Response.WriteAsync("</tbody>");
                    await context.Response.WriteAsync("</table>");
                }

                async Task GroupToTable(IEnumerable<IGrouping<string, DnsQuery>> groups, params string[] headings)
                {
                    var table = new DataTable();

                    foreach (var heading in headings)
                    {
                        table.Columns.Add(heading);
                    }

                    foreach (var group in groups.OrderByDescending(x => x.Count()).Take(20))
                    {
                        table.Rows.Add(group.Key, group.Count());
                    }
                    await WriteTable(table);
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

                if (context.Request.Path.StartsWithSegments("/reset"))
                {
                    Reset();
                    context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                    context.Response.Headers.Location = "/";
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
                    context.Response.ContentType = "text/html; charset=utf-8";

                    var startTimestamp = _lastReset;
                    if (context.Request.Query.ContainsKey("period"))
                    {
                        switch (context.Request.Query["period"])
                        {
                            case "hour":
                                startTimestamp = DateTime.UtcNow.AddHours(-1);
                                break;
                            case "day":
                                startTimestamp = DateTime.UtcNow.AddDays(-1);
                                break;
                            case "week":
                                startTimestamp = DateTime.UtcNow.AddDays(-7);
                                break;
                            case "month":
                                startTimestamp = DateTime.UtcNow.AddMonths(-1);
                                break;
                        }
                    }

                    var filteredQueries = _queries.Where(x => x.Created > startTimestamp).ToArray();
                    var answeredQueries = filteredQueries.Where(x => x.Answer != null);
                    var missingQueries = answeredQueries.Where(x => x.Answer.Header.ResponseCode == DnsResponseCode.NXDomain).ToArray();
                    var successfulQueries = answeredQueries.Where(x => x.Answer.Header.ResponseCode == DnsResponseCode.NoError).ToArray();
                    var refusedQueries = answeredQueries.Where(x => x.Answer.Header.ResponseCode == DnsResponseCode.Refused).ToArray();
                    var notAnsweredQueries = filteredQueries.Where(x => x.Answer == null).ToArray();

                    await context.Response.WriteAsync($"<h1>Metrics Server</h1>");
                    await context.Response.WriteAsync($"<p>" +
                        $"Metrics since {startTimestamp} (current time is {DateTime.UtcNow}). " +
                        $"There were {filteredQueries.Length} total queries, and there are {resolverCache.Count()} cache entries. " +
                        $"Of those queries, {successfulQueries.Length} were successful, {missingQueries.Length} were missing, " +
                        $"{refusedQueries.Length} were refused, and {notAnsweredQueries.Length} were not answered." +
                        $"</p>");
                    await context.Response.WriteAsync($"<p>Filter: Last <a href=\"/?period=hour\">hour</a>, <a href=\"/?period=day\">day</a>, <a href=\"/?period=week\">week</a>, <a href=\"/?period=month\">month</a>. Actions: <a href=\"/reset\" onclick=\"return confirm('Are you sure?')\">Reset statistics</a></p>");

                    await context.Response.WriteAsync($"<h2>Top Top Level Domains</h2>");
                    await context.Response.WriteAsync($"<p>Top level domains (permitted and refused).</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => string.Join('.', x.Query.Header.Host.Split('.').Reverse().First())), "Top Level Domain", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Refused Root Domains</h2>");
                    await context.Response.WriteAsync($"<p>Top domain names which were refused.</p>");
                    await GroupToTable(refusedQueries.GroupBy(x => string.Join('.', x.Query.Header.Host.Split('.').Reverse().Take(2).Reverse())), "Root Domain", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Permitted Root Domains</h2>");
                    await context.Response.WriteAsync($"<p>Top domain names which were permitted.</p>");
                    await GroupToTable(successfulQueries.GroupBy(x => string.Join('.', x.Query.Header.Host.Split('.').Reverse().Take(2).Reverse())), "Root Domain", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Missing Root Domains</h2>");
                    await context.Response.WriteAsync($"<p>Root domain names which were missing (NXDomain).</p>");
                    await GroupToTable(missingQueries.GroupBy(x => string.Join('.', x.Query.Header.Host.Split('.').Reverse().Take(2).Reverse())), "Root Domain", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Clients</h2>");
                    await context.Response.WriteAsync($"<p>Top DNS clients.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => x.Sender.Address.ToString()), "Client Address", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Responses</h2>");
                    await context.Response.WriteAsync($"<p>Top response codes for all queries.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => x.Answer?.Header.ResponseCode.ToString()), "Response Code", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Query Types</h2>");
                    await context.Response.WriteAsync($"<p>Top query types across all queries.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => x.Query.Header.QueryType.ToString()), "Query Type", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Answer Sources</h2>");
                    await context.Response.WriteAsync($"<p>Top sources of query responses in terms of the code or upstream which generated them.</p>");
                    await GroupToTable(answeredQueries.GroupBy(x => (x.Answer.Header.Tags.ContainsKey("Resolver") ? x.Answer.Header.Tags["Resolver"] : "<none>").ToString()), "Answer Source", "Hits");
                }
            });
        }

        private sealed class DnsQuery
        {
            public DnsMessage Query { get; set; }
            public DnsMessage? Answer { get; set; }
            public IPEndPoint Sender { get; set; }
            public TimeSpan? Elapsed { get; set; }
            public DateTime Created { get; set; }
        }

        private readonly ConcurrentBag<DnsQuery> _queries = new ConcurrentBag<DnsQuery>();
        private DateTime _lastReset = DateTime.UtcNow;

        private void Reset()
        {
            _queries.Clear();
            _lastReset = DateTime.UtcNow;
        }

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

                return default(TObject);
            }

            if (instrument.Meter.Name == DnsMetricsClient.MeterName)
            {
                var query = GetObjectFromTags<DnsMessage>(tags, "Query");
                var answer = GetObjectFromTags<DnsMessage>(tags, "Answer");
                var elapsed = GetObjectFromTags<TimeSpan?>(tags, "Elapsed");
                var sender = query.Header.Tags.TryGetValue("Sender", out var rawEndpoint) && rawEndpoint is IPEndPoint endpoint ? endpoint : throw new Exception();

                // Ensure we don't run out of memory
                if (_queries.Count > 1_000_000)
                {
                    Reset();
                }

                _queries.Add(new DnsQuery
                {
                    Query = query,
                    Answer = answer,
                    Sender = sender,
                    Elapsed = elapsed,
                    Created = DateTime.UtcNow
                });
            }
        }
    }
}
