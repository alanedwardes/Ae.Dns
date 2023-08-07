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

                    table.Columns.Add("Percentage");

                    var itemCounts = groups.Select(x => KeyValuePair.Create(x.Key, x.Count())).OrderByDescending(x => x.Value).ToList();
                    var totalCount = itemCounts.Sum(x => x.Value);

                    int CalculatePercentage(int count) => (int)(count / (double)totalCount * (double)100d);

                    foreach (var group in itemCounts.Take(20))
                    {
                        table.Rows.Add(group.Key, group.Value, CalculatePercentage(group.Value) + "%");
                    }

                    var remaining = itemCounts.Skip(20).Sum(x => x.Value);
                    if (remaining > 0)
                    {
                        table.Rows.Add("Other", remaining, CalculatePercentage(remaining) + "%");
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

                    IEnumerable<DnsQuery> query = _queries;

                    if (context.Request.Query.ContainsKey("period"))
                    {
                        switch (context.Request.Query["period"])
                        {
                            case "hour":
                                query = query.Where(x => x.Created > DateTime.UtcNow.AddHours(-1));
                                break;
                            case "day":
                                query = query.Where(x => x.Created > DateTime.UtcNow.AddDays(-1));
                                break;
                            case "week":
                                query = query.Where(x => x.Created > DateTime.UtcNow.AddDays(-7));
                                break;
                            case "month":
                                query = query.Where(x => x.Created > DateTime.UtcNow.AddMonths(-1));
                                break;
                        }
                    }

                    if (context.Request.Query.ContainsKey("sender") && IPAddress.TryParse(context.Request.Query["sender"], out IPAddress sender))
                    {
                        query = query.Where(x => x.Sender.Equals(sender));
                    }

                    if (context.Request.Query.ContainsKey("response") && Enum.TryParse(context.Request.Query["response"], out DnsResponseCode response))
                    {
                        query = query.Where(x => x.Answer?.ResponseCode == response);
                    }

                    if (context.Request.Query.ContainsKey("type") && Enum.TryParse(context.Request.Query["type"], out DnsQueryType type))
                    {
                        query = query.Where(x => x.Query.QueryType == type);
                    }

                    if (context.Request.Query.ContainsKey("resolver"))
                    {
                        query = query.Where(x => x.Answer?.Resolver == context.Request.Query["resolver"]);
                    }

                    var filteredQueries = query.ToArray();
                    var answeredQueries = filteredQueries.Where(x => x.Answer != null);
                    var missingQueries = answeredQueries.Where(x => x.Answer.ResponseCode == DnsResponseCode.NXDomain).ToArray();
                    var successfulQueries = answeredQueries.Where(x => x.Answer.ResponseCode == DnsResponseCode.NoError).ToArray();
                    var refusedQueries = answeredQueries.Where(x => x.Answer.ResponseCode == DnsResponseCode.Refused).ToArray();
                    var notAnsweredQueries = filteredQueries.Where(x => x.Answer == null).ToArray();

                    await context.Response.WriteAsync($"<h1>Metrics Server</h1>");
                    await context.Response.WriteAsync($"<p>Earliest tracked query was {filteredQueries.Select(x => x.Created).OrderBy(x => x).FirstOrDefault()} (current time is {DateTime.UtcNow}).</p>");
                    await context.Response.WriteAsync($"<p>There are {resolverCache.Count()} <a href=\"/cache\">cache entries</a>. {notAnsweredQueries.Length} queries were not answered.</p>");
                    await context.Response.WriteAsync($"<p>Filter: Last <a href=\"/?period=hour\">hour</a>, <a href=\"/?period=day\">day</a>, <a href=\"/?period=week\">week</a>, <a href=\"/?period=month\">month</a>.</p>");

                    var domainParts = 1;
                    if (context.Request.Query.ContainsKey("parts") && int.TryParse(context.Request.Query["parts"], out var parts))
                    {
                        domainParts = parts;
                    }

                    await context.Response.WriteAsync($"<h2>Top Domains</h2>");
                    await context.Response.WriteAsync($"<p>Showing the last {domainParts} parts of the domain. <a href=\"?parts={domainParts+1}\">More parts</a></p>");
                    await GroupToTable(filteredQueries.GroupBy(x => string.Join('.', x.Query.Host.Split('.').Reverse().Take(domainParts).Reverse())), "Domain", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Clients</h2>");
                    await context.Response.WriteAsync($"<p>Top DNS clients.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => $"<a href=\"?sender={x.Sender}\">{x.Sender}</a>"), "Client Address", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Responses</h2>");
                    await context.Response.WriteAsync($"<p>Top response codes for all queries.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => $"<a href=\"?response={x.Answer?.ResponseCode}\">{x.Answer?.ResponseCode}</a>"), "Response Code", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Query Types</h2>");
                    await context.Response.WriteAsync($"<p>Top query types across all queries.</p>");
                    await GroupToTable(filteredQueries.GroupBy(x => $"<a href=\"?type={x.Query.QueryType}\">{x.Query.QueryType}</a>"), "Query Type", "Hits");

                    await context.Response.WriteAsync($"<h2>Top Answer Sources</h2>");
                    await context.Response.WriteAsync($"<p>Top sources of query responses in terms of the code or upstream which generated them.</p>");
                    await GroupToTable(answeredQueries.GroupBy(x => $"<a href=\"?resolver={x.Answer.Resolver}\">{x.Answer.Resolver}</a>"), "Answer Source", "Hits");
                }
            });
        }

        private sealed class DnsHeaderLight
        {
            public DnsHeaderLight(DnsHeader header)
            {
                ResponseCode = header.ResponseCode;
                QueryType = header.QueryType;
                Host = header.Host;
                Resolver = header.Tags.ContainsKey("Resolver") ? header.Tags["Resolver"].ToString() : null;
            }

            public DnsResponseCode ResponseCode { get; }
            public DnsQueryType QueryType { get; }
            public string Host { get; }
            public string? Resolver { get; }
        }

        private sealed class DnsQuery
        {
            public DnsHeaderLight Query { get; set; }
            public DnsHeaderLight? Answer { get; set; }
            public IPAddress Sender { get; set; }
            public TimeSpan? Elapsed { get; set; }
            public DateTime Created { get; set; }
        }

        private readonly ConcurrentQueue<DnsQuery> _queries = new();

        private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
        {
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

                _queries.Enqueue(new DnsQuery
                {
                    Query = new DnsHeaderLight(query.Header),
                    Answer = new DnsHeaderLight(answer?.Header),
                    Sender = sender.Address,
                    Elapsed = elapsed,
                    Created = DateTime.UtcNow
                });

                if (_queries.Count > 100_000)
                {
                    _queries.TryDequeue(out DnsQuery _);
                }
            }
        }
    }
}
