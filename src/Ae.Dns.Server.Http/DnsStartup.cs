using Microsoft.AspNetCore.Builder;

namespace Ae.Dns.Server.Http
{
    /// <summary>
    /// The startup class for the <see cref="DnsHttpServer"/>.
    /// </summary>
    public class DnsStartup
    {
        /// <summary>
        /// Configure the <see cref="DnsMiddleware"/>.
        /// </summary>
        /// <param name="app"></param>
        public void Configure(IApplicationBuilder app) => app.UseMiddleware<DnsMiddleware>();
    }
}
