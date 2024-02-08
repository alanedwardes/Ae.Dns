﻿using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Accepts messages with <see cref="DnsOperationCode.UPDATE"/>, and stores the record for use elsewhere.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsUpdateClient : IDnsClient
    {
        private readonly IDnsZone _dnsZone;

        /// <summary>
        /// Create the new <see cref="DnsUpdateClient"/> using the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="dnsZone"></param>
        public DnsUpdateClient(IDnsZone dnsZone)
        {
            _dnsZone = dnsZone;
        }

        /// <inheritdoc/>
        public Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            query.EnsureOperationCode(DnsOperationCode.UPDATE);

            var hostnames = query.Nameservers.Select(x => x.Host).ToArray();

            void ChangeRecords(ICollection<DnsResourceRecord> records)
            {
                foreach (var recordToRemove in records.Where(x => hostnames.Contains(x.Host)).ToArray())
                {
                    records.Remove(recordToRemove);
                }

                foreach (var nameserver in query.Nameservers)
                {
                    records.Add(nameserver);
                }
            };

            if (query.Nameservers.Count > 0 && hostnames.Any(x => x.Last() != _dnsZone.Origin))
            {
                lock (_dnsZone)
                {
                    ChangeRecords(_dnsZone.Records);
                }

                return Task.FromResult(query.CreateAnswerMessage(DnsResponseCode.NoError, ToString()));
            }

            return Task.FromResult(query.CreateAnswerMessage(DnsResponseCode.Refused, ToString()));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
