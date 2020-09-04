using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsTcpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesCom()
        {
            using (var client = new DnsTcpClient(new NullLogger<DnsTcpClient>(), IPAddress.Parse("1.1.1.1")))
            {
                await client.Query(DnsHeader.CreateQuery("alanedwardes.com"), CancellationToken.None);
            }
        }
    }
}
