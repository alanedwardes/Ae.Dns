using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Provides methods to serialize and deserialize <see cref="IDnsZone"/>.
    /// </summary>
    public static class DnsZoneSerializer
    {
        /// <inheritdoc/>
        public static string SerializeZone(IDnsZone zone)
        {
            var writer = new StringBuilder();
            writer.AppendLine($"$ORIGIN {zone.Origin}.");

            if (zone.DefaultTtl.HasValue)
            {
                writer.AppendLine($"$TTL {(int)zone.DefaultTtl.Value.TotalSeconds}");
            }

            foreach (var record in zone.Records)
            {
                writer.AppendLine(SerializeRecord(record, zone));
            }

            return writer.ToString();
        }

        /// <inheritdoc/>
        public static void DeserializeZone(IDnsZone result, string zone)
        {
            var reader = new StringReader(zone);

            string? spillage = null;
            string? line;
            bool spillover = false;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Split(';')[0];

                if (line.Contains("(") && !line.Contains(")"))
                {
                    spillover = true;
                }

                if (line.Contains(")"))
                {
                    line = spillage + line;
                    spillover = false;
                }

                if (spillover)
                {
                    spillage += line;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("$ORIGIN"))
                {
                    result.Origin = line.Substring("$ORIGIN".Length).Trim().Trim('.');
                    continue;
                }

                if (line.StartsWith("$TTL"))
                {
                    result.DefaultTtl = TimeSpan.FromSeconds(int.Parse(line.Substring("$TTL".Length)));
                    continue;
                }

                var record = new DnsResourceRecord();
                DeserializeRecord(record, result, line);
                result.Records.Add(record);

                // Reset state
                spillover = false;
                spillage = null;
            }
        }

        /// <inheritdoc/>
        public static string SerializeRecord(DnsResourceRecord record, IDnsZone zone)
        {
            return string.Join(" ", new object?[] { zone.ToFormattedHost(record.Host), record.TimeToLive, record.Class, record.Type, SerializeResource(record.Resource, zone) }.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x?.ToString())));
        }

        /// <inheritdoc/>
        public static void DeserializeRecord(DnsResourceRecord record, IDnsZone zone, string input)
        {
            var parts = input.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

            var index = 0;

            if (char.IsWhiteSpace(input.First()))
            {
                record.Host = zone.Records.Last().Host;
            }
            else
            {
                record.Host = zone.FromFormattedHost(parts[index++]);
            }

            if (uint.TryParse(parts[index], out var ttl))
            {
                record.TimeToLive = ttl;
                index++;
            }
            else
            {
                record.TimeToLive = (uint)(zone.DefaultTtl?.TotalSeconds ?? throw new ArgumentNullException("No default TTL for the zone"));
            }

            if (Enum.TryParse<DnsQueryClass>(parts[index], out var cl))
            {
                record.Class = cl;
                index++;
            }
            else
            {
                record.Class = zone.Records.Last().Class;
            }

            record.Type = (DnsQueryType)Enum.Parse(typeof(DnsQueryType), parts[index++]);
            record.Resource = DnsResourceFactory.CreateResource(record.Type);
            DeserializeResource(record.Resource, zone, string.Join(" ", parts.Skip(index)));
        }

        /// <summary>
        /// Serialize the specified <see cref="IDnsResource"/> to a string.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static string SerializeResource(IDnsResource? record, IDnsZone zone)
        {
            if (record == null)
            {
                return string.Empty;
            }

            switch (record)
            {
                case DnsIpAddressResource ipAddress:
                    return ipAddress.IPAddress.ToString();
                case DnsMxResource mxResource:
                    return $"{mxResource.Preference} {zone.ToFormattedHost(mxResource.Entries)}";
                case DnsTextResource textResource:
                    if (textResource.Entries.Count == 1)
                    {
                        return textResource.Entries.Single();
                    }

                    return string.Join(" ", textResource.Entries.Select(x => $"\"{x}\""));
                case DnsSoaResource soaResource:
                    return string.Join(" ", new string[] { soaResource.MName + '.', soaResource.RName + '.', $"({soaResource.Serial} {(int)soaResource.Refresh.TotalSeconds} {(int)soaResource.Retry.TotalSeconds} {(int)soaResource.Expire.TotalSeconds} {(int)soaResource.Minimum.TotalSeconds})" });
                case DnsDomainResource domainResource:
                    return zone.ToFormattedHost(domainResource.Entries);
                case DnsUnknownResource unknownResource:
                    return $"({Convert.ToBase64String(unknownResource.Raw.ToArray())})";
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserialize into the specified <see cref="IDnsResource"/> from the specified string.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="zone"></param>
        /// <param name="input"></param>
        public static void DeserializeResource(IDnsResource? record, IDnsZone zone, string input)
        {
            if (record == null)
            {
                return;
            }

            switch (record)
            {
                case DnsIpAddressResource ipAddress:
                    ipAddress.IPAddress = IPAddress.Parse(input);
                    break;
                case DnsMxResource mxResource:
                    var mxParts = input.Split(null);
                    mxResource.Preference = ushort.Parse(mxParts[0]);
                    mxResource.Entries = zone.FromFormattedHost(mxParts[1]);
                    break;
                case DnsTextResource textResource:
                    var textParts = input.Split('"').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    textResource.Entries = new DnsLabels(textParts);
                    break;
                case DnsSoaResource soaResource:
                    var soaParts = input.Split(null, 3);
                    soaResource.MName = soaParts[0].Trim('.');
                    soaResource.RName = soaParts[1].Trim('.');

                    var soaValueParts = soaParts[2].Trim(new[] { '(', ')' }).Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                    soaResource.Serial = uint.Parse(soaValueParts[0]);
                    soaResource.Refresh = TimeSpan.FromSeconds(int.Parse(soaValueParts[1]));
                    soaResource.Retry = TimeSpan.FromSeconds(int.Parse(soaValueParts[2]));
                    soaResource.Expire = TimeSpan.FromSeconds(int.Parse(soaValueParts[3]));
                    soaResource.Minimum = TimeSpan.FromSeconds(uint.Parse(soaValueParts[4]));
                    break;
                case DnsDomainResource domainResource:
                    domainResource.Entries = zone.FromFormattedHost(input);
                    break;
                case DnsUnknownResource unknownResource:
                    var base64 = input.Trim().Trim(new char[] { '(', ')' });
                    unknownResource.Raw = Convert.FromBase64String(base64);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
