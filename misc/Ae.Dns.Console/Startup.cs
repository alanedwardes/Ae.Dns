using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Ae.Dns.Console
{
    public sealed class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes("Test"));
            });
        }
    }
}
