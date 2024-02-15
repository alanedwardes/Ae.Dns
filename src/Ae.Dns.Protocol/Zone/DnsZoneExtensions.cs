using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Extensions around <see cref="IDnsZone"/>.
    /// </summary>
    public static class DnsZoneExtensions
    {
        /// <inheritdoc/>
        public static string FromFormattedHost(this IDnsZone zone, string host)
        {
            if (host == "@")
            {
                return zone.Origin;
            }
            else if (host.EndsWith("."))
            {
                return host.Substring(0, host.Length - 1);
            }
            else
            {
                return host + "." + zone.Origin;
            }
        }

        /// <inheritdoc/>
        public static string ToFormattedHost(this IDnsZone zone, string host)
        {
            if (Regex.IsMatch(host, @"\s"))
            {
                throw new InvalidOperationException($"Hostname '{host}' contains invalid characters");
            }

            if (host == zone.Origin)
            {
                return "@";
            }
            else if (host.EndsWith(zone.Origin))
            {
                return host.Substring(0, host.Length - zone.Origin.ToString().Length - 1);
            }
            else
            {
                return host + '.';
            }
        }

        /// <summary>
        /// Check that this resource record is a valid <see cref="DnsSoaResource"/> record.
        /// </summary>
        /// <param name="dnsResourceRecord"></param>
        /// <returns></returns>
        public static DnsResourceRecord EnsureValidSoaRecord(DnsResourceRecord dnsResourceRecord)
        {
            if (!IsValidSoaRecord(dnsResourceRecord))
            {
                throw new InvalidOperationException($"Invalid SOA record: {dnsResourceRecord}");
            }

            return dnsResourceRecord;
        }

        /// <summary>
        /// Check that this resource record is a valid <see cref="DnsSoaResource"/> record.
        /// </summary>
        /// <param name="dnsResourceRecord"></param>
        /// <returns></returns>
        public static bool IsValidSoaRecord(DnsResourceRecord dnsResourceRecord)
        {
            if (dnsResourceRecord.Type != DnsQueryType.SOA)
            {
                return false;
            }

            if (dnsResourceRecord.Resource == null)
            {
                return false;
            }

            if (!(dnsResourceRecord.Resource is DnsSoaResource _))
            {
                return false;
            }

            return true;
        }

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
                else if (rr.Class == zone.GetZoneClass())
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

        /// <summary>
        /// Perform record updates for the specified <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="updateMessage"></param>
        public static DnsResponseCode PerformZoneUpdates(this IDnsZone zone, DnsMessage updateMessage)
        {
            var updates = updateMessage.Nameservers;

            // Pre-scan
            foreach (var rr in updates)
            {
                if (!rr.Host.ToString().EndsWith(zone.Origin))
                {
                    return DnsResponseCode.NotZone;
                }

                if (rr.Class == zone.GetZoneClass())
                {
                    if (new[] { DnsQueryType.ANY, DnsQueryType.AXFR, DnsQueryType.MAILA, DnsQueryType.MAILB }.Contains(rr.Type))
                    {
                        return DnsResponseCode.FormErr;
                    }
                }
                else if (rr.Class == DnsQueryClass.QCLASS_ANY)
                {
                    if (rr.TimeToLive != 0 || rr.Resource != null || new[] { DnsQueryType.AXFR, DnsQueryType.MAILA, DnsQueryType.MAILB }.Contains(rr.Type))
                    {
                        return DnsResponseCode.FormErr;
                    }
                }
                else if (rr.Class == DnsQueryClass.QCLASS_NONE)
                {
                    if (rr.TimeToLive != 0 || new[] { DnsQueryType.ANY, DnsQueryType.AXFR, DnsQueryType.MAILA, DnsQueryType.MAILB }.Contains(rr.Type))
                    {
                        return DnsResponseCode.FormErr;
                    }
                }
                else
                {
                    return DnsResponseCode.FormErr;
                }
            }

            // Perform updates
            foreach (var rr in updates)
            {
                if (rr.Class == zone.GetZoneClass())
                {
                    // TODO rewrite this
                    var existingRecords = zone.Records.Where(x => x.Host == rr.Host && x.Type == rr.Type).ToArray();
                    if (existingRecords.Any())
                    {
                        foreach (var existingRecord in existingRecords)
                        {
                            existingRecord.Resource = rr.Resource;
                            existingRecord.TimeToLive = rr.TimeToLive;
                        }
                    }
                    else
                    {
                        zone.Records.Add(rr);
                    }
                }
                else if (rr.Class == DnsQueryClass.QCLASS_ANY)
                {
                    if (rr.Type == DnsQueryType.ANY)
                    {
                        if (rr.Host == zone.Origin)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            foreach (var recordToRemove in zone.Records.Where(x => x.Host == rr.Host))
                            {
                                zone.Records.Remove(recordToRemove);
                            }
                        }
                    }
                    else if (rr.Host == zone.Origin && rr.Type == DnsQueryType.SOA || rr.Type == DnsQueryType.NS)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (var recordToRemove in zone.Records.Where(x => x.Host == rr.Host && x.Type == rr.Type))
                        {
                            zone.Records.Remove(recordToRemove);
                        }
                    }
                }
                else if (rr.Class == DnsQueryClass.QCLASS_NONE)
                {
                    if (rr.Type == DnsQueryType.SOA)
                    {
                        continue;
                    }
                    if (rr.Type == DnsQueryType.NS)
                    {
                        throw new NotImplementedException();
                    }

                    foreach (var recordToRemove in zone.Records.Where(x => x.Host == rr.Host && x.Type == rr.Type && Equals(x.Resource, rr.Resource)).ToArray())
                    {
                        zone.Records.Remove(recordToRemove);
                    }
                }
            }

            return DnsResponseCode.NoError;
        }

        /// <summary>
        /// Get the class of the <see cref="IDnsZone"/>.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static DnsQueryClass GetZoneClass(this IDnsZone zone)
        {
            // TODO: force SOA in IDnsZone and use that...
            if (zone.Records.Any())
            {
                return zone.Records.First().Class;
            }

            return DnsQueryClass.IN;
        }
    }
}
