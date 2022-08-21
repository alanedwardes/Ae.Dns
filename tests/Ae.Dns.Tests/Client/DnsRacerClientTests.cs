using Ae.Dns.Client;
using Ae.Dns.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client
{
    public class DnsRacerClientTests
    {
        private class DnsTestClient : IDnsClient
        {
            private readonly Func<Task<DnsMessage>> _func;
            public DnsTestClient(Func<Task<DnsMessage>> func) => _func = func;

            public void Dispose()
            {
            }

            public Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default) => _func();
        }

        [Fact]
        public async Task TestUseFastestResult()
        {
            var expectedAnswer = new DnsMessage();

            using var fastClient = new DnsTestClient(() => Task.FromResult(expectedAnswer));
            using var slowClient = new DnsTestClient(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new DnsMessage();
            });

            var racer = new DnsRacerClient(new[] { fastClient, slowClient });

            Assert.Same(expectedAnswer, await racer.Query(new DnsMessage(), CancellationToken.None));
        }

        [Fact]
        public async Task TestUseNonFaultedResult()
        {
            var expectedAnswer = new DnsMessage();

            using var successClient = new DnsTestClient(() => Task.FromResult(expectedAnswer));
            using var errorClient = new DnsTestClient(async () => throw new InvalidOperationException());

            var racer = new DnsRacerClient(new[] { successClient, errorClient });

            for (int i = 0; i < 20; i++)
            {
                Assert.Same(expectedAnswer, await racer.Query(new DnsMessage(), CancellationToken.None));
            }
        }

        [Fact]
        public async Task TestAllFaultedResults()
        {
            using var errorClient = new DnsTestClient(async () => throw new InvalidOperationException());

            var racer = new DnsRacerClient(new[] { errorClient, errorClient });

            await Assert.ThrowsAsync<InvalidOperationException>(() => racer.Query(new DnsMessage(), CancellationToken.None));
        }
    }
}
