using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Console
{
    internal static class CacheHandler
    {
        internal static Task RemoveAsync(string key, ObjectCache cache, HttpContext context)
        {
            cache.Remove(key);
            context.Response.Redirect("/cache");
            return Task.CompletedTask;
        }

        internal static async Task ListAsync(HttpContext context, ObjectCache cache)
        {
            var entries = cache
                .Where(x => x.Value is DnsCachingClient.DnsCacheEntry)
                .Select(x => KeyValuePair.Create(x.Key, (DnsCachingClient.DnsCacheEntry)x.Value))
                .OrderBy(x => x.Value.Expires)
                .ToList();

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);
            await context.Response.WriteAsync($"<h1>{cache.Name}</h1>");
            await context.Response.WriteAsync($"<p>Cached Objects = {entries.Count}</p>");
            await context.Response.WriteAsync("<table><thead><tr><th>Cache Key</th><th>Expires</th><th>Hits</th><th>Response Code</th><th>Actions</th></tr></thead><tbody>");

            foreach (var entry in entries)
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

            await context.Response.WriteAsync("</tbody></table>");
        }
    }
}
