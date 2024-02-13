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
    }
}
