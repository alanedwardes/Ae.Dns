using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsHttpClientTests
    {
        [Fact]
        public async Task TestLookupAlanEdwardesComWithCloudFlare()
        {
            IDnsClient client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://cloudflare-dns.com/") });

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"));

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogle()
        {
            IDnsClient client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"));

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupCpscGovWithGoogle()
        {
            IDnsClient client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));

            Assert.Equal(29, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupGovUkWithGoogle()
        {
            IDnsClient client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("gov.uk", DnsQueryType.ANY));

            Assert.Equal(33, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogleAny()
        {
            IDnsClient client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com", DnsQueryType.ANY));

            Assert.Equal(12, answer.Answers.Count);
        }
    }
}
