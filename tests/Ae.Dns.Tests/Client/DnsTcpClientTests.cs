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
            DnsAnswer answer;
            using (var client = new DnsTcpClient(new NullLogger<DnsTcpClient>(), IPAddress.Parse("1.1.1.1")))
            {
                answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"), CancellationToken.None);
            }

            Assert.Equal(4, answer.Answers.Count);
        }
    }
}
