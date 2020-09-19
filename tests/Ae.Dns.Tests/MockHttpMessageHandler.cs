using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Tests
{
    public class MockHttpMessageHandler : DelegatingHandler
    {
        public virtual Task<HttpResponseMessage> SendAsyncMock(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => SendAsyncMock(request, cancellationToken);
    }
}
