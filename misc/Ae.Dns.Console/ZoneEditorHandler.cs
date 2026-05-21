using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Console
{
    internal static class ZoneEditorHandler
    {
        internal static async Task ListAsync(HttpContext context, ZoneRegistry registry)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);
            await context.Response.WriteAsync("<p><a href=\"/\">← Home</a></p>");
            await context.Response.WriteAsync("<h1>Zones</h1><ul>");
            foreach (var entry in registry.Zones)
            {
                var origin = entry.Zone.Origin.ToString();
                await context.Response.WriteAsync($"<li><a href=\"/zones/{Uri.EscapeDataString(origin)}\">{WebUtility.HtmlEncode(origin)}</a> — {entry.Zone.Records.Count} records</li>");
            }
            await context.Response.WriteAsync("</ul>");
        }

        internal static async Task DetailAsync(string origin, HttpContext context, ZoneRegistry registry)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);

            var zoneEntry = FindZone(registry, origin);
            if (zoneEntry == null) { await NotFound(context, origin); return; }

            var zone = zoneEntry.Zone;
            var zoneUrl = ZoneUrl(origin);
            var errorMsg = context.Request.Query.TryGetValue("error", out var errVal) ? errVal.ToString() : null;
            var records = zone.Records.ToList();
            var defaultTtl = (int)(zone.DefaultTtl?.TotalSeconds ?? 3600);

            await context.Response.WriteAsync("<p><a href=\"/zones\">← Zones</a></p>");
            await context.Response.WriteAsync($"<h1>Zone: {WebUtility.HtmlEncode(origin)}</h1>");
            if (errorMsg != null)
                await context.Response.WriteAsync($"<p style=\"color:red\">{WebUtility.HtmlEncode(errorMsg)}</p>");

            await context.Response.WriteAsync("<table><thead><tr><th>Name</th><th>TTL</th><th>Type</th><th>Value</th><th></th></tr></thead><tbody>");
            for (int i = 0; i < records.Count; i++)
            {
                var rec = records[i];
                await context.Response.WriteAsync("<tr>");
                await context.Response.WriteAsync($"<td>{WebUtility.HtmlEncode(zone.ToFormattedHost(rec.Host))}</td>");
                await context.Response.WriteAsync($"<td>{rec.TimeToLive}</td>");
                await context.Response.WriteAsync($"<td>{rec.Type}</td>");
                await context.Response.WriteAsync($"<td>{WebUtility.HtmlEncode(DnsZoneSerializer.SerializeResource(rec.Resource, zone))}</td>");
                await context.Response.WriteAsync($"<td><a href=\"{zoneUrl}/edit/{i}\">Edit</a> ");
                await context.Response.WriteAsync($"<form method=\"POST\" action=\"{zoneUrl}/delete/{i}\"><button type=\"submit\">Delete</button></form></td>");
                await context.Response.WriteAsync("</tr>");
            }
            await context.Response.WriteAsync("</tbody></table>");

            await context.Response.WriteAsync("<h2>Add Record</h2>");
            await context.Response.WriteAsync($"<form method=\"POST\" action=\"{zoneUrl}/add\" style=\"display:block\">");
            await context.Response.WriteAsync("<p><label>Name<br><input name=\"host\" placeholder=\"@ or www or mail.example.com.\"></label></p>");
            await context.Response.WriteAsync($"<p><label>TTL<br><input name=\"ttl\" type=\"number\" value=\"{defaultTtl}\"></label></p>");
            await context.Response.WriteAsync("<p><label>Type<br><select name=\"type\">");
            foreach (var t in new[] { "A", "AAAA", "CNAME", "MX", "NS", "PTR", "TEXT", "SOA", "CAA" })
                await context.Response.WriteAsync($"<option>{t}</option>");
            await context.Response.WriteAsync("</select></label></p>");
            await context.Response.WriteAsync("<p><label>Value<br><input name=\"value\" style=\"width:40em\" placeholder=\"e.g. 1.2.3.4 for A, 10 mail.example.com. for MX\"></label></p>");
            await context.Response.WriteAsync("<button type=\"submit\">Add</button></form>");
        }

        internal static async Task AddAsync(string origin, HttpContext context, ZoneRegistry registry)
        {
            var zoneEntry = FindZone(registry, origin);
            if (zoneEntry == null) { await NotFound(context, origin); return; }

            var zone = zoneEntry.Zone;
            var form = await context.Request.ReadFormAsync();
            var host = form["host"].ToString().Trim();
            var ttl = form["ttl"].ToString().Trim();
            var type = form["type"].ToString().Trim();
            var value = form["value"].ToString().Trim();

            try
            {
                var rec = new DnsResourceRecord();
                DnsZoneSerializer.DeserializeRecord(rec, zone, $"{host} {ttl} IN {type} {value}");
                await zone.Update(() => { zone.Records.Add(rec); return true; });
                await File.WriteAllTextAsync(zoneEntry.FilePath, DnsZoneSerializer.SerializeZone(zone));
                context.Response.Redirect(ZoneUrl(origin));
            }
            catch (Exception ex)
            {
                context.Response.Redirect($"{ZoneUrl(origin)}?error={Uri.EscapeDataString(ex.Message)}");
            }
        }

        internal static async Task EditFormAsync(string origin, int index, HttpContext context, ZoneRegistry registry)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);

            var zoneEntry = FindZone(registry, origin);
            if (zoneEntry == null) { await NotFound(context, origin); return; }

            var zone = zoneEntry.Zone;
            if (index < 0 || index >= zone.Records.Count) { context.Response.StatusCode = 404; await context.Response.WriteAsync("<p>Record not found.</p>"); return; }

            var rec = zone.Records[index];
            await WriteEditForm(context.Response, origin, index,
                zone.ToFormattedHost(rec.Host),
                rec.TimeToLive.ToString(),
                rec.Type.ToString(),
                DnsZoneSerializer.SerializeResource(rec.Resource, zone),
                error: null);
        }

        internal static async Task EditSaveAsync(string origin, int index, HttpContext context, ZoneRegistry registry)
        {
            var zoneEntry = FindZone(registry, origin);
            if (zoneEntry == null) { await NotFound(context, origin); return; }

            var zone = zoneEntry.Zone;
            if (index < 0 || index >= zone.Records.Count) { context.Response.StatusCode = 404; return; }

            var form = await context.Request.ReadFormAsync();
            var editHost = form["host"].ToString();
            var editTtl = form["ttl"].ToString();
            var editType = zone.Records[index].Type.ToString();
            var editValue = form["value"].ToString();

            try
            {
                var rec = new DnsResourceRecord();
                DnsZoneSerializer.DeserializeRecord(rec, zone, $"{editHost} {editTtl} IN {editType} {editValue}");
                await zone.Update(() => { zone.Records[index] = rec; return true; });
                await File.WriteAllTextAsync(zoneEntry.FilePath, DnsZoneSerializer.SerializeZone(zone));
                context.Response.Redirect(ZoneUrl(origin));
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(Html.Css);
                await WriteEditForm(context.Response, origin, index, editHost, editTtl, editType, editValue, ex.Message);
            }
        }

        internal static async Task DeleteAsync(string origin, int index, HttpContext context, ZoneRegistry registry)
        {
            var zoneEntry = FindZone(registry, origin);
            if (zoneEntry == null) { await NotFound(context, origin); return; }

            var zone = zoneEntry.Zone;
            if (index >= 0 && index < zone.Records.Count)
            {
                await zone.Update(() => { zone.Records.RemoveAt(index); return true; });
                await File.WriteAllTextAsync(zoneEntry.FilePath, DnsZoneSerializer.SerializeZone(zone));
            }
            context.Response.Redirect(ZoneUrl(origin));
        }

        private static async Task WriteEditForm(HttpResponse response, string origin, int index, string host, string ttl, string type, string value, string? error)
        {
            var zoneUrl = ZoneUrl(origin);
            await response.WriteAsync($"<p><a href=\"{zoneUrl}\">← {WebUtility.HtmlEncode(origin)}</a></p>");
            await response.WriteAsync("<h1>Edit Record</h1>");
            if (error != null)
                await response.WriteAsync($"<p style=\"color:red\">{WebUtility.HtmlEncode(error)}</p>");
            await response.WriteAsync($"<form method=\"POST\" action=\"{zoneUrl}/edit/{index}\" style=\"display:block\">");
            await response.WriteAsync($"<p><label>Name<br><input name=\"host\" value=\"{WebUtility.HtmlEncode(host)}\"></label></p>");
            await response.WriteAsync($"<p><label>TTL<br><input name=\"ttl\" type=\"number\" value=\"{WebUtility.HtmlEncode(ttl)}\"></label></p>");
            await response.WriteAsync($"<p><label>Type<br><input value=\"{WebUtility.HtmlEncode(type)}\" readonly></label></p>");
            await response.WriteAsync($"<p><label>Value<br><input name=\"value\" value=\"{WebUtility.HtmlEncode(value)}\" style=\"width:40em\"></label></p>");
            await response.WriteAsync($"<button type=\"submit\">Save</button> <a href=\"{zoneUrl}\">Cancel</a>");
            await response.WriteAsync("</form>");
        }

        private static ZoneRegistry.ZoneEntry? FindZone(ZoneRegistry registry, string origin) =>
            registry.Zones.FirstOrDefault(z => z.Zone.Origin.ToString() == origin);

        private static string ZoneUrl(string origin) =>
            $"/zones/{Uri.EscapeDataString(origin)}";

        private static async Task NotFound(HttpContext context, string origin)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"<p>Zone not found: {WebUtility.HtmlEncode(origin)}</p>");
        }
    }
}
