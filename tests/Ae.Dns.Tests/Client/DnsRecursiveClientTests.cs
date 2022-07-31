using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsRecursiveClientTests
    {
        [Theory]
        [InlineData("argos.co.uk")]
        [InlineData("pages.github.com")]
        [InlineData("twitter.com")]
        [InlineData("news.bbc.co.uk")]
        [InlineData("www.theguardian.com")]
        //[InlineData("ae-infrastructure-eu-west-1.s3.eu-west-1.amazonaws.com")] TODO investigate
        [InlineData("news.ycombinator.com")]
        [InlineData("asimov.vortex.data.trafficmanager.net")]
        public async Task RecursiveLookup(string domain)
        {
            using var client = new DnsRecursiveClient();

            var answer = await client.Query(DnsQueryFactory.CreateQuery(domain));

            Assert.Empty(answer.Additional);
            Assert.NotEmpty(answer.Answers);
        }
    }
}

