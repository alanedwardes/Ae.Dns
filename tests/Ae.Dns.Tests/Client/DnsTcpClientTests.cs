using Ae.Dns.Client;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsTcpClientTests
    {
        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithCloudFlare(string domain, DnsQueryType type)
        {
            using var client = new DnsTcpClient(IPAddress.Parse("1.1.1.1"));
            await client.RunQuery(domain, type, type == DnsQueryType.ANY ? DnsResponseCode.NotImp : DnsResponseCode.NoError);
        }

        [Theory]
        [ClassData(typeof(LookupTestCases))]
        public async Task TestLookupWithGoogle(string domain, DnsQueryType type)
        {
            using var client = new DnsTcpClient(IPAddress.Parse("8.8.8.8"));
            await client.RunQuery(domain, type);
        }
    }
}
