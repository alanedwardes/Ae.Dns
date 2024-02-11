using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Zone;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// A query processor backed by <see cref="IDnsZone"/>.
    /// </summary>
    public sealed class DnsZoneClient : IDnsClient
    {
        private readonly IDnsClient _dnsClient;
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Construct a new <see cref="DnsZoneClient"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="dnsClient"></param>
        /// <param name="dnsZone"></param>
        public DnsZoneClient(IDnsClient dnsClient, IDnsZone dnsZone)
        {
            _dnsClient = dnsClient;
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            // If this query is not relevant to us
            if (!query.Header.Host.ToString().EndsWith(_dnsZone.Origin) || _dnsZone.Records.Count == 0)
            {
                return await _dnsClient.Query(query, token);
            }

            var relevantRecords = _dnsZone.Records
                .Where(x => x.Host == query.Header.Host &&
                            x.Class == query.Header.QueryClass)
                .ToArray();

            if (relevantRecords.Length > 0)
            {
                var exactRecords = relevantRecords.Where(x => x.Type == query.Header.QueryType).ToArray();
                if (exactRecords.Length > 0)
                {
                    var answer = DnsMessageExtensions.CreateAnswerMessage(query, DnsResponseCode.NoError, ToString());
                    answer.Answers = exactRecords;
                    answer.Header.AnswerRecordCount = (short)answer.Answers.Count;
                    return answer;
                }
                else
                {
                    // TODO: return SOA record
                    return DnsMessageExtensions.CreateAnswerMessage(query, DnsResponseCode.NoError, ToString());
                }
            }

            return DnsMessageExtensions.CreateAnswerMessage(query, DnsResponseCode.NXDomain, ToString());
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsZoneClient)}({_dnsZone})";
    }
}
