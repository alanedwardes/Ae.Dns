using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsRecursiveClientTests
    {
        [Fact]
        public async Task Test()
        {
            var client = new DnsRecursiveClient();

            var t = DnsRootServers.Random;


            var test = await client.Query(DnsQueryFactory.CreateQuery("argos.co.uk"));
        }
    }
}

