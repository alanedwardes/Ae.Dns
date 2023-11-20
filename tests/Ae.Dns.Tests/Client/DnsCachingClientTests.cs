using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public sealed class DnsCachingClientTests : IDisposable
    {
        private readonly MockRepository _repository = new MockRepository(MockBehavior.Strict);

        public void Dispose() => _repository.VerifyAll();

        [Fact]
        public async Task TestCacheEntryWithOnlyAdditionalRecords()
        {
            var mockClient = _repository.Create<IDnsClient>();

            var query1 = DnsQueryFactory.CreateQuery("example.com", DnsQueryType.A);

            // query1 will be passed through to the inner client
            mockClient.Setup(x => x.Query(query1, CancellationToken.None))
                .ReturnsAsync(new DnsMessage
                {
                    Additional = new List<DnsResourceRecord>
                    {
                        // This is an unrealistic example
                        new DnsResourceRecord
                        {
                            Host = new DnsLabels("example.com"),
                            Type = DnsQueryType.A,
                            Resource = new DnsIpAddressResource{IPAddress = IPAddress.Loopback},
                            TimeToLive = 5
                        }
                    },
                    Header = new DnsHeader { AdditionalRecordCount = 1, Id = query1.Header.Id }
                });

            using var cache = new MemoryCache("wibble");

            IDnsClient client = new DnsCachingClient(mockClient.Object, cache);

            Assert.Empty(cache);

            var answer1 = await client.Query(query1, CancellationToken.None);

            Assert.Equal(query1.Header.Id, answer1.Header.Id);
            Assert.Single(cache);

            // Will be served from the cache
            var query2 = DnsQueryFactory.CreateQuery("example.com", DnsQueryType.A);
            var answer2 = await client.Query(query2, CancellationToken.None);

            Assert.Equal(query2.Header.Id, answer2.Header.Id);
            Assert.Single(cache);
        }
    }
}
