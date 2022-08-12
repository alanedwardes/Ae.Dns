using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A client which logs metrics aganst DNS responses.
    /// </summary>
    public sealed class DnsMetricsClient : IDnsClient
    {
        private static readonly Meter _meter = new Meter("Ae.Dns.Client.DnsMetricsClient");
        private static readonly Counter<int> _missingCounter = _meter.CreateCounter<int>("Missing");
        private static readonly Counter<int> _refusedCounter = _meter.CreateCounter<int>("Refused");
        private static readonly Counter<int> _otherCounter = _meter.CreateCounter<int>("Other");

        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsMetricsClient"/> using the specified <see cref="IDnsClient"/>.
        /// </summary>
        public DnsMetricsClient(IDnsClient dnsClient) => _dnsClient = dnsClient;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsAnswer> Query(DnsHeader query, CancellationToken token = default)
        {
            var queryMetricState = new KeyValuePair<string, object>("Query", query);

            var answer = await _dnsClient.Query(query, token);

            switch (answer.Header.ResponseCode)
            {
                case DnsResponseCode.NoError:
                    break;
                case DnsResponseCode.NXDomain:
                    _missingCounter.Add(1, queryMetricState);
                    break;
                case DnsResponseCode.Refused:
                    _refusedCounter.Add(1, queryMetricState);
                    break;
                default:
                    _otherCounter.Add(1, queryMetricState);
                    break;
            }

            return answer;
        }
    }
}
