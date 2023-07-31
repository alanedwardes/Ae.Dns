using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Linq;

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

                var soaResource = (DnsSoaResource)soaRecord.Resource;
                return TimeSpan.FromSeconds(Math.Min(soaRecord.TimeToLive, (uint)soaResource.Minimum.TotalSeconds));
            }

            return null;
        }
    }
}
