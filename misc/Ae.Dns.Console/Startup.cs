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
using Microsoft.AspNetCore.WebUtilities;
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
                        listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
            meterListener.Start();

            app.UseMiddleware<DnsMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/cache/remove/{key}", CacheHandler.RemoveAsync);
                endpoints.MapGet("/cache", CacheHandler.ListAsync);
                endpoints.MapGet("/captures/on", CapturesHandler.EnableAsync);
                endpoints.MapGet("/captures/off", CapturesHandler.DisableAsync);
                endpoints.MapGet("/captures/clear", CapturesHandler.ClearAsync);
                endpoints.MapGet("/captures", CapturesHandler.ListAsync);
                endpoints.MapGet("/zones", ZoneEditorHandler.ListAsync);
                endpoints.MapGet("/zones/{origin}", ZoneEditorHandler.DetailAsync);
                endpoints.MapPost("/zones/{origin}/add", ZoneEditorHandler.AddAsync);
                endpoints.MapGet("/zones/{origin}/edit/{index:int}", ZoneEditorHandler.EditFormAsync);
                endpoints.MapPost("/zones/{origin}/edit/{index:int}", ZoneEditorHandler.EditSaveAsync);
                endpoints.MapPost("/zones/{origin}/delete/{index:int}", ZoneEditorHandler.DeleteAsync);
                endpoints.MapGet("/", HandleHomeAsync);
            });
        }

        private async Task HandleHomeAsync(HttpContext context, IDnsClient dnsClient, ObjectCache resolverCache, ZoneRegistry? zoneRegistry)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);

            string CreateQueryString(string name, object? value)
            {
                IDictionary<string, string?> filters = context.Request.Query.ToDictionary(x => x.Key, x => (string?)x.Value.ToString());
                var valueString = value?.ToString();
                if (valueString != null)
                    filters[name] = valueString;
                return QueryHelpers.AddQueryString(context.Request.Path, filters);
            }

            string CreateQueryStringWithout(string name) =>
                QueryHelpers.AddQueryString(context.Request.Path, context.Request.Query.Where(x => x.Key != name));

            IEnumerable<DnsQuery> query = _queries.OrderByDescending(x => x.Created);

            if (context.Request.Query.ContainsKey("sender") && IPAddress.TryParse(context.Request.Query["sender"], out IPAddress? sender))
                query = query.Where(x => x.Sender.Equals(sender));

            if (context.Request.Query.ContainsKey("response") && Enum.TryParse(context.Request.Query["response"], out DnsResponseCode response))
                query = query.Where(x => x.Answer?.ResponseCode == response);

            if (context.Request.Query.ContainsKey("type") && Enum.TryParse(context.Request.Query["type"], out DnsQueryType type))
                query = query.Where(x => x.Query.QueryType == type);

            if (context.Request.Query.ContainsKey("resolver"))
                query = query.Where(x => x.Answer?.Resolver == context.Request.Query["resolver"]);

            if (context.Request.Query.ContainsKey("upstream") && bool.TryParse(context.Request.Query["upstream"], out bool upstream))
                query = query.Where(x => x.Answer?.Upstream == upstream);

            if (context.Request.Query.ContainsKey("server"))
                query = query.Where(x => x.Query.Server == context.Request.Query["server"]);

            if (context.Request.Query.ContainsKey("blockReason"))
                query = query.Where(x => x.Query.BlockReason == context.Request.Query["blockReason"]);

            var hostFilter = context.Request.Query.TryGetValue("hostFilter", out var rawHostFilter) ? rawHostFilter.ToString() : "suffix";

            if (context.Request.Query.ContainsKey("host"))
            {
                switch (hostFilter)
                {
                    case "match":
                        query = query.Where(x => x.Query.Host.Contains(context.Request.Query["host"]!, StringComparison.InvariantCultureIgnoreCase));
                        break;
                    case "suffix":
                        query = query.Where(x => x.Query.Host.EndsWith(context.Request.Query["host"]!, StringComparison.InvariantCultureIgnoreCase));
                        break;
                }
            }

            var filteredQueries = query.ToArray();

            var reverseLookups = (await Task.WhenAll(
                filteredQueries
                    .Where(x => x.Sender != null)
                    .Select(x => x.Sender)
                    .Distinct()
                    .Select(async x =>
                    {
                        var answer = await dnsClient.Query(DnsQueryFactory.CreateReverseQuery(x));
                        return (x, answer.Answers.FirstOrDefault()?.Resource?.ToString());
                    })
            )).ToDictionary(x => x.x, x => x.Item2);

            var pageLimit = context.Request.Query.TryGetValue("limit", out var rawLimit) && int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 20;

            string SenderFilter(DnsQuery q) => $"<a href=\"{CreateQueryString("sender", q.Sender)}\">{reverseLookups[q.Sender] ?? q.Sender.ToString()}</a>";
            string? ResponseFilter(DnsQuery q) => q.Answer == null ? null : $"<a href=\"{CreateQueryString("response", q.Answer.ResponseCode)}\">{q.Answer.ResponseCode}</a>";
            string QueryTypeFilter(DnsQuery q) => $"<a href=\"{CreateQueryString("type", q.Query.QueryType)}\">{q.Query.QueryType}</a>";
            string? ResolverFilter(DnsQuery q) => q.Answer == null ? null : $"<a href=\"{CreateQueryString("resolver", q.Answer.Resolver)}\">{q.Answer.Resolver}</a>";
            string? UpstreamFilter(DnsQuery q) => q.Answer == null ? null : $"<a href=\"{CreateQueryString("upstream", q.Answer.Upstream)}\">{q.Answer.Upstream}</a>";
            string ServerFilter(DnsQuery q) => $"<a href=\"{CreateQueryString("server", q.Query.Server)}\">{q.Query.Server}</a>";
            string BlockReasonFilter(DnsQuery q) => $"<a href=\"{CreateQueryString("blockReason", q.Query.BlockReason)}\">{q.Query.BlockReason}</a>";
            string HostSuffixFilter(string host) => $"<a href=\"{CreateQueryString("host", host)}\">{host}</a>";

            async Task GroupToTable(IEnumerable<IGrouping<string?, DnsQuery>> groups, params string[] headings)
            {
                var table = new DataTable();
                foreach (var heading in headings) table.Columns.Add(heading);
                table.Columns.Add("Percentage");

                var itemCounts = groups.Select(x => KeyValuePair.Create(x.Key, x.Count())).OrderByDescending(x => x.Value).ToList();
                var totalCount = itemCounts.Sum(x => x.Value);
                int Pct(int count) => (int)(count / (double)totalCount * 100d);

                foreach (var group in itemCounts.Take(pageLimit))
                    table.Rows.Add(group.Key, group.Value, Pct(group.Value) + "%");

                var remaining = itemCounts.Skip(pageLimit).Sum(x => x.Value);
                if (remaining > 0)
                    table.Rows.Add("Other", remaining, Pct(remaining) + "%");

                await WriteTableAsync(context.Response, table);
            }

            await context.Response.WriteAsync("<h1>Metrics Server</h1>");
            if (zoneRegistry != null && zoneRegistry.Zones.Count > 0)
                await context.Response.WriteAsync($"<p><a href=\"/zones\">Zones ({zoneRegistry.Zones.Count})</a></p>");
            await context.Response.WriteAsync($"<p>Earliest tracked query was {filteredQueries.Select(x => x.Created).LastOrDefault()} (current time is {DateTime.UtcNow}).</p>");
            await context.Response.WriteAsync($"<p>There are {resolverCache.Count()} <a href=\"/cache\">cache entries</a>. {filteredQueries.Count(x => x.Answer == null)} queries were not answered.</p>");

            if (context.Request.Query.Count > 0)
                await context.Response.WriteAsync($"<p>Filters applied: {string.Join(", ", context.Request.Query.Select(x => $"<a href=\"{CreateQueryStringWithout(x.Key)}\">{x.Key} is {x.Value}</a>"))}.</p>");

            await context.Response.WriteAsync("<form action=\"\" method=\"GET\">");
            foreach (var item in context.Request.Query.Where(x => x.Key != "hostFilter" && x.Key != "host"))
                await context.Response.WriteAsync($"<input type=\"hidden\" name=\"{item.Key}\" value=\"{item.Value}\"/>");
            await context.Response.WriteAsync($"<input placeholder=\"Query\" type=\"text\" name=\"host\" value=\"{(context.Request.Query.TryGetValue("host", out var rawHost) ? rawHost.ToString() : "")}\"/>");
            await context.Response.WriteAsync($"<select name=\"hostFilter\">");
            await context.Response.WriteAsync($"<option value=\"match\" {(hostFilter == "match" ? "selected" : string.Empty)}>Match</option>");
            await context.Response.WriteAsync($"<option value=\"suffix\" {(hostFilter == "suffix" ? "selected" : string.Empty)}>Suffix</option>");
            await context.Response.WriteAsync("</select>");
            await context.Response.WriteAsync("<input type=\"submit\" value=\"Submit\"/></form>");

            var domainParts = context.Request.Query.TryGetValue("parts", out var rawParts) && int.TryParse(rawParts, out var parsedParts) ? parsedParts : 1;

            await context.Response.WriteAsync($"<h2>Top Hosts</h2><p>Showing the last {domainParts} parts of the domain. <a href=\"{CreateQueryString("parts", domainParts + 1)}\">More parts</a></p>");
            await GroupToTable(filteredQueries.GroupBy(x => HostSuffixFilter(string.Join('.', x.Query.Host.Split('.').Reverse().Take(domainParts).Reverse()))), "Host", "Hits");

            await context.Response.WriteAsync("<h2>Top Clients</h2><p>Top DNS clients.</p>");
            await GroupToTable(filteredQueries.GroupBy(SenderFilter), "Client Address", "Hits");

            await context.Response.WriteAsync("<h2>Top Responses</h2><p>Top response codes for all queries.</p>");
            await GroupToTable(filteredQueries.GroupBy(ResponseFilter), "Response Code", "Hits");

            await context.Response.WriteAsync("<h2>Top Query Types</h2><p>Top query types across all queries.</p>");
            await GroupToTable(filteredQueries.GroupBy(QueryTypeFilter), "Query Type", "Hits");

            await context.Response.WriteAsync("<h2>Top Answer Sources</h2><p>Top sources of query responses in terms of the code which generated them.</p>");
            await GroupToTable(filteredQueries.GroupBy(ResolverFilter), "Answer Source", "Hits");

            await context.Response.WriteAsync("<h2>Upstream Used</h2><p>Whether an upstream generated the response or not.</p>");
            await GroupToTable(filteredQueries.GroupBy(UpstreamFilter), "Upstream Used", "Hits");

            await context.Response.WriteAsync("<h2>Top Servers</h2><p>Top servers used.</p>");
            await GroupToTable(filteredQueries.GroupBy(ServerFilter), "Server", "Hits");

            await context.Response.WriteAsync("<h2>Top Block Reasons</h2><p>Top reasons queries are blocked.</p>");
            await GroupToTable(filteredQueries.GroupBy(BlockReasonFilter), "Block Reason", "Hits");

            var recentQueries = new DataTable { Columns = { "Timestamp", "Sender", "Duration", "Host", "Type", "Response" } };
            foreach (var q in filteredQueries.Take(pageLimit))
                recentQueries.Rows.Add(q.Created, SenderFilter(q), q.Elapsed?.TotalSeconds.ToString("F"), HostSuffixFilter(q.Query.Host), QueryTypeFilter(q), ResponseFilter(q));

            await context.Response.WriteAsync("<h2>Recent Queries</h2><p>50 most recent queries / answers.</p>");
            await WriteTableAsync(context.Response, recentQueries);
        }

        private static async Task WriteTableAsync(HttpResponse response, DataTable table)
        {
            await response.WriteAsync("<table><thead><tr>");
            foreach (DataColumn col in table.Columns)
                await response.WriteAsync($"<th>{col.ColumnName}</th>");
            await response.WriteAsync("</tr></thead><tbody>");
            foreach (DataRow row in table.Rows)
            {
                await response.WriteAsync("<tr>");
                foreach (var item in row.ItemArray)
                    await response.WriteAsync($"<td>{item}</td>");
                await response.WriteAsync("</tr>");
            }
            await response.WriteAsync("</tbody></table>");
        }

        private sealed class DnsHeaderLight
        {
            public DnsHeaderLight(DnsHeader header)
            {
                ResponseCode = header.ResponseCode;
                QueryType = header.QueryType;
                Host = header.Host;
                Resolver = (header.Tags.ContainsKey("Resolver") ? header.Tags["Resolver"].ToString() : null) ?? "Unknown";
                Upstream = header.Tags.ContainsKey("Upstream");
                BlockReason = (header.Tags.ContainsKey("BlockReason") ? header.Tags["BlockReason"].ToString() : null) ?? "None";
                Server = (header.Tags.ContainsKey("Server") ? header.Tags["Server"].ToString() : null) ?? "Unknown";
            }

            public DnsResponseCode ResponseCode { get; }
            public DnsQueryType QueryType { get; }
            public string Host { get; }
            public string Resolver { get; }
            public bool Upstream { get; }
            public string BlockReason { get; }
            public string Server { get; }
        }

        private sealed class DnsQuery
        {
            public DnsQuery(DnsHeaderLight query, IPAddress sender)
            {
                Query = query;
                Sender = sender;
            }

            public DnsHeaderLight Query { get; }
            public DnsHeaderLight? Answer { get; set; }
            public IPAddress Sender { get; }
            public TimeSpan? Elapsed { get; set; }
            public DateTime Created { get; } = DateTime.UtcNow;
        }

        private readonly ConcurrentQueue<DnsQuery> _queries = new();

        private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            static TObject? GetTag<TObject>(ReadOnlySpan<KeyValuePair<string, object?>> t, string name)
            {
                foreach (var tag in t)
                    if (tag.Key == name) return (TObject?)tag.Value;
                return default;
            }

            if (instrument.Meter.Name != DnsMetricsClient.MeterName)
                return;

            var queryMsg = GetTag<DnsMessage>(tags, "Query") ?? throw new NullReferenceException("No query specified");
            var answerMsg = GetTag<DnsMessage>(tags, "Answer");
            var elapsed = GetTag<TimeSpan?>(tags, "Elapsed");
            var sender = queryMsg.Header.Tags.TryGetValue("Sender", out var raw) && raw is IPEndPoint ep ? ep.Address : null;
            if (sender == null) return;

            _queries.Enqueue(new DnsQuery(new DnsHeaderLight(queryMsg.Header), sender)
            {
                Answer = answerMsg != null ? new DnsHeaderLight(answerMsg.Header) : null,
                Elapsed = elapsed
            });

            if (_queries.Count > 100_000)
                _queries.TryDequeue(out _);
        }
    }
}
