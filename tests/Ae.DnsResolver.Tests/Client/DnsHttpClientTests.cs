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

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com"));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupCpscGovWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(29, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupGovUkWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("gov.uk", DnsQueryType.ANY));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(33, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogleAny()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var result = await client.LookupRaw(DnsHeader.CreateQuery("alanedwardes.com", DnsQueryType.ANY));

            var offset = 0;
            var answer = result.ReadDnsAnswer(ref offset);

            Assert.Equal(12, answer.Answers.Count);
        }
    }
}
