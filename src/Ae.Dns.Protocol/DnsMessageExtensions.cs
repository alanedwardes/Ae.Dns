using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ae.Dns.Protocol
{
    internal static class DnsMessageExtensions
    {
        public static ushort? GetMaxUdpMessageSize(this DnsMessage message)
        {
            return (ushort?)message.Additional.FirstOrDefault(x => x.Type == DnsQueryType.OPT)?.Class;
        }

        public static TimeSpan? GetCachableTtl(this DnsMessage message)
        {
            if (message.Header.ResponseCode == DnsResponseCode.NoError)
            {
                var allRecords = new[] { message.Answers, message.Nameservers, message.Additional }.SelectMany(x => x).ToArray();
                if (allRecords.Length == 0)
                {
                    // There's nothing here to compute a TTL from
                    return null;
                }

                return TimeSpan.FromSeconds(allRecords.Min(x => x.TimeToLive));
            }

            if (message.Header.ResponseCode == DnsResponseCode.NXDomain)
            {
                var soaRecord = message.Nameservers.SingleOrDefault(x => x.Type == DnsQueryType.SOA);
                if (soaRecord == null)
                {
                    // The NXDomain result was non-standard
                    return null;
                }

                var soaResource = (DnsSoaResource?)soaRecord.Resource;
                if (soaResource == null)
                {
                    // The SOA resource is broken
                    return null;
                }

                return TimeSpan.FromSeconds(Math.Min(soaRecord.TimeToLive, (uint)soaResource.Minimum.TotalSeconds));
            }

            return null;
        }

        public static void EnsureOperationCode(this DnsMessage message, DnsOperationCode expected)
        {
            if (message.Header.OperationCode != expected)
            {
                throw new Exception($"The operation code {message.Header.ResponseCode} was not expected (needed {expected})");
            }
        }

        public static void EnsureQueryType(this DnsMessage message, DnsQueryType expected)
        {
            if (message.Header.QueryType != expected)
            {
                throw new Exception($"The query type {message.Header.QueryType} was not expected (needed {expected})");
            }
        }

        public static void EnsureHost(this DnsMessage message, DnsLabels expected)
        {
            if (message.Header.Host != expected)
            {
                throw new Exception($"The host {message.Header.Host} was not expected (needed {expected})");
            }
        }

        public static void EnsureSuccessResponseCode(this DnsMessage message)
        {
            if (message.Header.ResponseCode == DnsResponseCode.ServFail ||
                message.Header.ResponseCode == DnsResponseCode.NotImp ||
                message.Header.ResponseCode == DnsResponseCode.NotAuth)
            {
                throw new Exception($"The response code {message.Header.ResponseCode} does not indicate success for message {message}");
            }
        }

        public static bool TryParseIpAddressFromReverseLookup(this DnsMessage message, out IPAddress? address)
        {
            if (message.Header.QueryType == DnsQueryType.PTR && message.Header.Host.Count > 3
                && message.Header.Host.Last().Equals("arpa", StringComparison.InvariantCultureIgnoreCase))
            {
                var lookupType = message.Header.Host[message.Header.Host.Count - 2];
                if (lookupType.Equals("in-addr", StringComparison.InvariantCultureIgnoreCase))
                {
                    return IPAddress.TryParse(string.Join(".", message.Header.Host.Take(4).Reverse()), out address);
                }

                if (lookupType.Equals("ip6", StringComparison.InvariantCultureIgnoreCase))
                {
                    return IPAddress.TryParse(string.Join(":", Chunk(message.Header.Host.Take(32).Reverse(), 4).Select(x => string.Concat(x))), out address);
                }
            }

            address = null;
            return false;
        }

        private static IEnumerable<TSource[]> Chunk<TSource>(IEnumerable<TSource> source, int size)
        {
#if NET6_0_OR_GREATER
            return source.Chunk(size);
#else
            using IEnumerator<TSource> e = source.GetEnumerator();
            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                int i = 1;
                for (; i < chunk.Length && e.MoveNext(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
#endif
        }

        public static DnsMessage CreateAnswerMessage(this DnsMessage query, DnsResponseCode responseCode, string resolver) => new DnsMessage
        {
            Header = new DnsHeader
            {
                Id = query.Header.Id,
                ResponseCode = responseCode,
                IsQueryResponse = true,
                RecursionAvailable = true,
                RecursionDesired = query.Header.RecursionDesired,
                Host = query.Header.Host,
                QueryClass = query.Header.QueryClass,
                QuestionCount = query.Header.QuestionCount,
                QueryType = query.Header.QueryType,
                Tags = { { "Resolver", resolver } }
            }
        };

        /// <summary>
        /// RFC 2136 2.4
        /// </summary>
        public enum ZoneUpdatePreRequisite
        {
            Unknown,
            /// <summary>
            /// At least one RR with a specified NAME and TYPE (in the zone and class specified in the Zone Section) must exist. (RFC 2136 2.4.1)
            /// </summary>
            RRsetExistsValueIndependent,
            /// <summary>
            /// A set of RRs with a specified NAME and TYPE exists and has the same members with the same RDATAs as the RRset specified here in this section. (RFC 2136 2.4.2)
            /// </summary>
            RRsetExistsValueDependent,
            /// <summary>
            /// No RRs with a specified NAME and TYPE (in the zone and class denoted by the Zone Section) can exist. (RFC 2136 2.4.3)
            /// </summary>
            RRsetDoesNotExist,
            /// <summary>
            /// Name is in use.  At least one RR with a specified NAME (in the zone and class specified by the Zone Section) must exist. (RFC 2136 2.4.4)
            /// </summary>
            NameIsInUse,
            /// <summary>
            /// Name is not in use.  No RR of any type is owned by a specified NAME. (RFC 2136 2.4.5)
            /// </summary>
            NameIsNotInUse
        }

        /// <summary>
        /// Return the zone update type, if this is a <see cref="DnsOperationCode.UPDATE"/> operation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ZoneUpdatePreRequisite GetZoneUpdatePreRequisite(this DnsMessage message)
        {
            var preRequisites = message.Answers;
            if (preRequisites.Count == 0 || preRequisites.Any(x => x.TimeToLive != 0))
            {
                // At least one pre-req is expected, and TTL always must be zero
                return ZoneUpdatePreRequisite.Unknown;
            }

            if (preRequisites.Count == 1)
            {
                var preRequisite = preRequisites.Single();

                // For this prerequisite, a requestor adds to the section a single RR
                // whose NAME and TYPE are equal to that of the zone RRset whose
                // existence is required.RDLENGTH is zero and RDATA is therefore
                // empty.  CLASS must be specified as ANY to differentiate this
                // condition from that of an actual RR whose RDLENGTH is naturally zero
                // (0)(e.g., NULL).TTL is specified as zero (0).
                if (preRequisite.Class == DnsQueryClass.QCLASS_ANY && preRequisite.Type != DnsQueryType.ANY)
                {
                    return ZoneUpdatePreRequisite.RRsetExistsValueIndependent;
                }

                // For this prerequisite, a requestor adds to the section a single RR
                // whose NAME and TYPE are equal to that of the RRset whose nonexistence
                // is required.The RDLENGTH of this record is zero(0), and RDATA
                // field is therefore empty.  CLASS must be specified as NONE in order
                // to distinguish this condition from a valid RR whose RDLENGTH is
                // naturally zero (0)(for example, the NULL RR).TTL must be specified
                // as zero (0).
                if (preRequisite.Class == DnsQueryClass.QCLASS_NONE && preRequisite.Type != DnsQueryType.ANY)
                {
                    return ZoneUpdatePreRequisite.RRsetDoesNotExist;
                }

                // For this prerequisite, a requestor adds to the section a single RR
                // whose NAME is equal to that of the name whose ownership of an RR is
                // required.RDLENGTH is zero and RDATA is therefore empty.  CLASS must
                // be specified as ANY to differentiate this condition from that of an
                // actual RR whose RDLENGTH is naturally zero (0)(e.g., NULL).TYPE
                // must be specified as ANY to differentiate this case from that of an
                // RRset existence test.TTL is specified as zero (0).
                if (preRequisite.Class == DnsQueryClass.QCLASS_ANY && preRequisite.Type == DnsQueryType.ANY)
                {
                    return ZoneUpdatePreRequisite.NameIsInUse;
                }

                // For this prerequisite, a requestor adds to the section a single RR
                // whose NAME is equal to that of the name whose nonownership of any RRs
                // is required.RDLENGTH is zero and RDATA is therefore empty.  CLASS
                // must be specified as NONE.TYPE must be specified as ANY.TTL must
                // be specified as zero (0).
                if (preRequisite.Class == DnsQueryClass.QCLASS_NONE && preRequisite.Type == DnsQueryType.ANY)
                {
                    return ZoneUpdatePreRequisite.NameIsNotInUse;
                }
            }

            // For this prerequisite, a requestor adds to the section an entire
            // RRset whose preexistence is required.NAME and TYPE are that of the
            // RRset being denoted.  CLASS is that of the zone.  TTL must be
            // specified as zero (0) and is ignored when comparing RRsets for
            // identity.
            return ZoneUpdatePreRequisite.RRsetExistsValueDependent;
        }

        /// <summary>
        /// RFC 2136 2.5
        /// </summary>
        public enum ZoneUpdateType
        {
            Unknown,
            /// <summary>
            /// RRs are added to the Update Section whose NAME, TYPE, TTL, RDLENGTH and RDATA are those being added, and CLASS is the same as the zone class.  Any duplicate RRs will be silently ignored by the primary master. (RFC 2136 2.5.1)
            /// </summary>
            AddToAnRRset,
            /// <summary>
            /// One RR is added to the Update Section whose NAME and TYPE are those of the RRset to be deleted.  TTL must be specified as zero (0) and is otherwise not used by the primary master.CLASS must be specified as
            /// ANY.RDLENGTH must be zero(0) and RDATA must therefore be empty. If no such RRset exists, then this Update RR will be silently ignored by the primary master. (RFC 2136 2.5.2)
            /// </summary>
            DeleteAnRRset,
            /// <summary>
            /// One RR is added to the Update Section whose NAME is that of the name to be cleansed of RRsets.  TYPE must be specified as ANY.  TTL must be specified as zero (0) and is otherwise not used by the primary
            /// master.CLASS must be specified as ANY.RDLENGTH must be zero(0) and RDATA must therefore be empty.If no such RRsets exist, then this Update RR will be silently ignored by the primary master. (RFC 2136 2.5.3)
            /// </summary>
            DeleteAllRRsetsFromAName,
            /// <summary>
            /// RRs to be deleted are added to the Update Section.  The NAME, TYPE, RDLENGTH and RDATA must match the RR being deleted.  TTL must be specified as zero (0) and will otherwise be ignored by the primary 
            /// master.CLASS must be specified as NONE to distinguish this from an RR addition.  If no such RRs exist, then this Update RR will be silently ignored by the primary master. (RFC 2136 2.5.4)
            /// </summary>
            DeleteAnRRFromAnRRset
        }

        /// <summary>
        /// Get the zone update type specified by the message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ZoneUpdateType GetZoneUpdateType(this DnsMessage message)
        {
            var updates = message.Nameservers;

            if (updates.Count == 0)
            {
                return ZoneUpdateType.Unknown;
            }

            if (updates.Count == 1)
            {
                var update = updates.Single();

                if (update.Type != DnsQueryType.ANY && update.Class == DnsQueryClass.QCLASS_ANY)
                {
                    return ZoneUpdateType.DeleteAnRRset;
                }

                if (update.Type == DnsQueryType.ANY && update.Class == DnsQueryClass.QCLASS_ANY)
                {
                    return ZoneUpdateType.DeleteAllRRsetsFromAName;
                }
            }

            if (updates.All(x => x.Class == DnsQueryClass.QCLASS_NONE))
            {
                return ZoneUpdateType.DeleteAnRRFromAnRRset;
            }

            return ZoneUpdateType.AddToAnRRset;
        }
    }
}
