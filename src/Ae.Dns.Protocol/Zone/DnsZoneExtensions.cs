using Ae.Dns.Protocol.Enums;
using System.Linq;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Extensions around <see cref="IDnsZone"/>.
    /// </summary>
    public static class DnsZoneExtensions
    {
        /// <summary>
        /// Test the pre-requisites specified in the <see cref="DnsMessage"/>.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="updateMessage"></param>
        public static DnsResponseCode TestZoneUpdatePreRequisites(this IDnsZone zone, DnsMessage updateMessage)
        {
            var prerequisites = updateMessage.Answers;

            // This is almost a line by line copy of RFC 2136
            // 3.2.5 - Pseudocode for Prerequisite Section Processing
            foreach (var rr in prerequisites)
            {
                if (rr.TimeToLive != 0)
                {
                    return DnsResponseCode.FormErr;
                }

                if (!rr.Host.ToString().EndsWith(zone.Origin))
                {
                    return DnsResponseCode.NotZone;
                }

                if (rr.Class == DnsQueryClass.QCLASS_ANY)
                {
                    if (rr.Resource != null)
                    {
                        return DnsResponseCode.FormErr;
                    }
                    if (rr.Type == DnsQueryType.ANY)
                    {
                        if (!zone.Records.Any(x => x.Host == rr.Host))
                        {
                            return DnsResponseCode.NXDomain;
                        }
                    }
                    else
                    {
                        if (!zone.Records.Any(x => x.Host == rr.Host && x.Type == rr.Type))
                        {
                            return DnsResponseCode.NXRRSet;
                        }
                    }
                }
                else if (rr.Class == DnsQueryClass.QCLASS_NONE)
                {
                    if (rr.Resource != null)
                    {
                        return DnsResponseCode.FormErr;
                    }
                    if (rr.Type == DnsQueryType.ANY)
                    {
                        if (zone.Records.Any(x => x.Host == rr.Host))
                        {
                            return DnsResponseCode.YXDomain;
                        }
                    }
                    else
                    {
                        if (zone.Records.Any(x => x.Host == rr.Host && x.Type == rr.Type))
                        {
                            return DnsResponseCode.YXRRSet;
                        }
                    }
                }
                else if (rr.Class == zone.Records.First().Class)
                {
                    if (!zone.Records.Any(x => x.Host == rr.Host && x.Type == rr.Type && Equals(x.Resource, rr.Resource)))
                    {
                        return DnsResponseCode.NXRRSet;
                    }
                }
                else
                {
                    return DnsResponseCode.FormErr;
                }
            }

            return DnsResponseCode.NoError;
        }
    }
}
