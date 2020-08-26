using Ae.DnsResolver.Client;
using Ae.DnsResolver.Protocol;
using Ae.DnsResolver.Protocol.Enums;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ae.DnsResolver.Tests.Client
{
    public class DnsHttpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesComWithCloudFlare()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://cloudflare-dns.com/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com"));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Length);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com"));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Length);
        }

        [Fact]
        public async Task TestLookupCpscGovWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            foreach (var test in answer.Answers)
            {
                var test1 = test.DataOffset;
                var test2 = result.ReadString(ref test1);
            }

            Assert.Equal(29, answer.Answers.Length);
        }
    }
}
