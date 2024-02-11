using Ae.Dns.Protocol;
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
            var relevantRecords = _dnsZone.Records
                .Where(x => x.Host == query.Header.Host &&
                            x.Class == query.Header.QueryClass &&
                            x.Type == query.Header.QueryType)
                .ToArray();

            if (relevantRecords.Length > 0)
            {
                var answer = DnsMessageExtensions.CreateAnswerMessage(query, Protocol.Enums.DnsResponseCode.NoError, ToString());
                answer.Answers = relevantRecords;
                answer.Header.AnswerRecordCount = (short)answer.Answers.Count;
                return answer;
            }

            return await _dnsClient.Query(query, token);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(DnsZoneClient)}({_dnsZone})";
    }
}
