using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Intercepts HTTP requests, resolving them to one or more IP addresses and making the request directly to the IP address with a host header.
    /// </summary>
    public sealed class DnsDelegatingHandler : DelegatingHandler
    {
        private readonly IDnsClient _dnsClient;
        private readonly DnsQueryType _queryType;

        public DnsDelegatingHandler(IDnsClient dnsClient, bool internetProtocolV4 = true)
        {
            _dnsClient = dnsClient;
            _queryType = internetProtocolV4 ? DnsQueryType.A : DnsQueryType.AAAA;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var originalHostHeader = request.Headers.Host;
            var originalHost = request.RequestUri.Host;

            // Make a DNS request for the host
            var answers = await _dnsClient.Query(DnsHeader.CreateQuery(originalHost, _queryType), cancellationToken);
            if (!answers.Answers.Any())
            {
                throw new InvalidOperationException($"Unable to resolve {_queryType} records for host {originalHost}");
            }

            // Pick a random IP address
            var addressResource = (DnsIpAddressResource)answers.Answers.OrderBy(x => Guid.NewGuid()).First().Resource;

            // Set the request data to talk to the IP address directly
            request.RequestUri = ChangeHost(request.RequestUri, addressResource.IPAddress.ToString());
            request.Headers.Host = originalHost;

            var response = await base.SendAsync(request, cancellationToken);

            // Restore the original request data
            request.RequestUri = ChangeHost(request.RequestUri, originalHost);
            request.Headers.Host = originalHostHeader;

            return response;
        }

        private static Uri ChangeHost(Uri uri, string newHost) => new UriBuilder(uri) { Host = newHost }.Uri;
    }
}
