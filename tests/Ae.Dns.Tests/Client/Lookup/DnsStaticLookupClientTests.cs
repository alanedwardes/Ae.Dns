using Ae.Dns.Client.Lookup;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DnsStaticLookupClientTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Strict);

        public void Dispose() => _mockRepository.VerifyAll();

        [Fact]
        public async Task TestForwardLookup()
        {
            var innerClient = _mockRepository.Create<IDnsClient>();

            var source = _mockRepository.Create<IDnsLookupSource>();

            IList<IPAddress> addresses = new List<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
            source.Setup(x => x.TryForwardLookup("wibble", out addresses))
                  .Returns(true);

            var client = new DnsStaticLookupClient(innerClient.Object, source.Object);

            var answer1 = await client.Query(DnsQueryFactory.CreateQuery("wibble", DnsQueryType.A));
            Assert.Equal(DnsResponseCode.NoError, answer1.Header.ResponseCode);
            var answer1record1 = Assert.Single(answer1.Answers);
            Assert.Equal(1, answer1.Header.AnswerRecordCount);
            Assert.Equal(IPAddress.Loopback, ((DnsIpAddressResource)answer1record1.Resource).IPAddress);

            var answer2 = await client.Query(DnsQueryFactory.CreateQuery("wibble", DnsQueryType.AAAA));
            Assert.Equal(DnsResponseCode.NoError, answer2.Header.ResponseCode);
            var answer2record1 = Assert.Single(answer2.Answers);
            Assert.Equal(1, answer2.Header.AnswerRecordCount);
            Assert.Equal(IPAddress.IPv6Loopback, ((DnsIpAddressResource)answer2record1.Resource).IPAddress);

            var answer3 = await client.Query(DnsQueryFactory.CreateQuery("wibble", DnsQueryType.MX));
            Assert.Equal(DnsResponseCode.NoError, answer3.Header.ResponseCode);
            Assert.Equal(0, answer3.Header.AnswerRecordCount);
            Assert.Empty(answer3.Answers);
        }

        [Fact]
        public async Task TestReverseLookup()
        {
            var innerClient = _mockRepository.Create<IDnsClient>();

            var source = _mockRepository.Create<IDnsLookupSource>();

            IList<string> hostnamesv6 = new List<string> { "wibblev6" };
            source.Setup(x => x.TryReverseLookup(IPAddress.IPv6Loopback, out hostnamesv6))
                  .Returns(true);

            IList<string> hostnamesv4 = new List<string> { "wibblev4" };
            source.Setup(x => x.TryReverseLookup(IPAddress.Loopback, out hostnamesv4))
                  .Returns(true);

            var client = new DnsStaticLookupClient(innerClient.Object, source.Object);

            var answer1 = await client.Query(DnsQueryFactory.CreateReverseQuery(IPAddress.Loopback));
            Assert.Equal(DnsResponseCode.NoError, answer1.Header.ResponseCode);
            var answer1record1 = Assert.Single(answer1.Answers);
            Assert.Equal(1, answer1.Header.AnswerRecordCount);
            Assert.Equal("wibblev4", Assert.Single(((DnsTextResource)answer1record1.Resource).Entries));

            var answer2 = await client.Query(DnsQueryFactory.CreateReverseQuery(IPAddress.IPv6Loopback));
            Assert.Equal(DnsResponseCode.NoError, answer2.Header.ResponseCode);
            var answer2record1 = Assert.Single(answer2.Answers);
            Assert.Equal(1, answer2.Header.AnswerRecordCount);
            Assert.Equal("wibblev6", Assert.Single(((DnsTextResource)answer2record1.Resource).Entries));
        }

        [Fact]
        public async Task TestPassthroughQuery()
        {
            var innerClient = _mockRepository.Create<IDnsClient>();

            var passthroughQuery = DnsQueryFactory.CreateQuery("wibble", DnsQueryType.A);

            innerClient.Setup(x => x.Query(passthroughQuery, CancellationToken.None))
                       .ReturnsAsync(passthroughQuery);

            var source = _mockRepository.Create<IDnsLookupSource>();

            IList<IPAddress> addresses = null;
            source.Setup(x => x.TryForwardLookup("wibble", out addresses))
                  .Returns(false);

            var client = new DnsStaticLookupClient(innerClient.Object, source.Object);

            Assert.Same(passthroughQuery, await client.Query(passthroughQuery, CancellationToken.None));
        }
    }
}
