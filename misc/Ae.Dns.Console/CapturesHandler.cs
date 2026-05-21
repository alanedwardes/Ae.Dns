using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.AspNetCore.Http;

namespace Ae.Dns.Console
{
    internal static class CapturesHandler
    {
        internal static Task EnableAsync(DnsCaptureRawClient captureClient, HttpContext context)
        {
            captureClient.IsEnabled = true;
            context.Response.Redirect("/captures");
            return Task.CompletedTask;
        }

        internal static Task DisableAsync(DnsCaptureRawClient captureClient, HttpContext context)
        {
            captureClient.IsEnabled = false;
            context.Response.Redirect("/captures");
            return Task.CompletedTask;
        }

        internal static Task ClearAsync(DnsCaptureRawClient captureClient, HttpContext context)
        {
            captureClient.Captures.Clear();
            context.Response.Redirect("/captures");
            return Task.CompletedTask;
        }

        internal static async Task ListAsync(HttpContext context, DnsCaptureRawClient captureClient)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Html.Css);
            await context.Response.WriteAsync("<h1>Captures</h1>");
            await context.Response.WriteAsync($"<p>Captures are {(captureClient.IsEnabled ? "on" : "off")}, captured = {captureClient.Captures.Count}</p>");
            await context.Response.WriteAsync($"<p>Controls: <a href=\"/captures/{(captureClient.IsEnabled ? "off" : "on")}\">{(captureClient.IsEnabled ? "Stop" : "Start")} Capturing</a> <a href=\"/captures/clear\">Clear Captures</a></p>");
            await context.Response.WriteAsync("<ul>");

            foreach (var capture in captureClient.Captures)
            {
                await context.Response.WriteAsync("<li>");
                await context.Response.WriteAsync($"<b>Served by {capture.Request.ServerName} from {capture.Request.SourceEndpoint}</b>");
                await context.Response.WriteAsync($"<pre>{capture.Response?.Query.ToString() ?? "Error processing query"}</pre>");
                await context.Response.WriteAsync($"<pre>{DnsByteExtensions.ToDebugString(capture.Query)}</pre>");

                if (capture.Answer.HasValue)
                {
                    await context.Response.WriteAsync($"<pre>{capture.Response?.Answer}</pre>");
                    await context.Response.WriteAsync($"<pre>{DnsByteExtensions.ToDebugString(capture.Answer.Value)}</pre>");
                }

                if (capture.Exception != null)
                    await context.Response.WriteAsync($"<pre>{capture.Exception}</pre>");

                await context.Response.WriteAsync("</li>");
            }

            await context.Response.WriteAsync("</ul>");
        }
    }
}
