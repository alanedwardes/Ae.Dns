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

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"));

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com"));

            Assert.Equal(4, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupCpscGovWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("cpsc.gov", DnsQueryType.ANY));

            Assert.Equal(29, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupGovUkWithGoogle()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("gov.uk", DnsQueryType.ANY));

            Assert.Equal(33, answer.Answers.Count);
        }

        [Fact]
        public async Task TestLookupAlanEdwardesComWithGoogleAny()
        {
            var client = new DnsHttpClient(new HttpClient { BaseAddress = new Uri("https://dns.google/") });

            var answer = await client.Query(DnsHeader.CreateQuery("alanedwardes.com", DnsQueryType.ANY));

            Assert.Equal(12, answer.Answers.Count);
        }
    }
}
