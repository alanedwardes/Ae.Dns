using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.DnsResolver.Tests.Client
{
    public class DnsTcpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesCom()
        {
            byte[] result;
            using (var client = new DnsTcpClient(new NullLogger<DnsTcpClient>(), IPAddress.Parse("1.1.1.1"), "test"))
            {
                result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com"));
            }

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Count);
        }
    }
}
