using Microsoft.AspNetCore.Builder;

namespace Ae.Dns.Server.Http
{
    public class DnsStartup
    {
        public void Configure(IApplicationBuilder app) => app.UseMiddleware<DnsMiddleware>();
    }
}
