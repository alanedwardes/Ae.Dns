﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Ae.Dns.Protocol.Records.ServiceBinding;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// See https://datatracker.ietf.org/doc/html/draft-ietf-dnsop-svcb-https-10
    /// </summary>
    public sealed class DnsServiceBindingResource : IDnsResource, IEquatable<DnsServiceBindingResource>
    {
        /// <summary>
        /// When SvcPriority is 0 the SVCB record is in AliasMode. Otherwise, it is in ServiceMode.
        /// RRSets are explicitly unordered collections, so the SvcPriority field is used to impose an ordering on SVCB RRs.
        /// See https://www.ietf.org/archive/id/draft-ietf-dnsop-svcb-https-12.html#name-svcpriority
        /// </summary>
        public ushort SvcPriority { get; set; }

        /// <summary>
        /// In AliasMode, the SVCB record aliases a service to a TargetName.
        /// The primary purpose of AliasMode is to allow aliasing at the zone apex, where CNAME is not allowed.
        /// 
        /// For ServiceMode SVCB RRs, if TargetName has the value ".", then the owner name of this record MUST be used as the effective TargetName.
        /// https://www.ietf.org/archive/id/draft-ietf-dnsop-svcb-https-12.html#name-aliasmode
        /// </summary>
        public string[] TargetName { get; set; }

        /// <summary>
        /// The set of SVCB parameters contained within this record.
        /// </summary>
        public IDictionary<SvcParameter, IDnsResource> SvcParameters { get; set; } = new Dictionary<SvcParameter, IDnsResource>();

        /// <inheritdoc/>
        public bool Equals(DnsServiceBindingResource other)
        {
            return SvcPriority == other.SvcPriority &&
                TargetName.SequenceEqual(other.TargetName) &&
                SvcParameters.SequenceEqual(other.SvcParameters);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsServiceBindingResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(SvcPriority, TargetName, SvcParameters);

        private IDnsResource CreateResourceRecord(SvcParameter parameterType)
        {
            return parameterType switch
            {
                SvcParameter.Alpn => new SvcStringParameter(),
                SvcParameter.Port => new SvcUShortParameter(),
                SvcParameter.IPv4Hint => new SvcIpAddressesParameter(AddressFamily.InterNetwork),
                SvcParameter.IPv6Hint => new SvcIpAddressesParameter(AddressFamily.InterNetworkV6),
                SvcParameter.Dohpath => new SvcUtf8StringParameter(),
                _ => new DnsUnknownResource(),
            };
        }

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            var totalLength = offset + length;

            SvcPriority = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            TargetName = DnsByteExtensions.ReadString(bytes, ref offset, false);

            while (offset < totalLength)
            {
                var parameterType = (SvcParameter)DnsByteExtensions.ReadUInt16(bytes, ref offset);
                var parameterLength = DnsByteExtensions.ReadUInt16(bytes, ref offset);

                var parameterResource = CreateResourceRecord(parameterType);
                parameterResource.ReadBytes(bytes, ref offset, parameterLength);
                SvcParameters.Add(parameterType, parameterResource);
            }
        }

        /// <inheritdoc/>
        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            DnsByteExtensions.ToBytes(SvcPriority, bytes, ref offset);
            DnsByteExtensions.ToBytes(TargetName, bytes, ref offset);

            foreach (var parameter in SvcParameters.OrderBy(x => x.Key))
            {
                DnsByteExtensions.ToBytes((short)parameter.Key, bytes, ref offset);

                int resourceLength = 0;
                parameter.Value.WriteBytes(bytes.Slice(offset + sizeof(short)), ref resourceLength);

                DnsByteExtensions.ToBytes((short)resourceLength, bytes, ref offset);

                offset += resourceLength;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var parts = new List<string>
            {
                SvcPriority.ToString()
            };
            
            if (TargetName.Length == 0)
            {
                parts.Add(".");
            }
            else
            {
                parts.Add(string.Join(".", TargetName) + '.');
            }

            // Matches what is supported by dig
            var labelMap = new Dictionary<SvcParameter, string>
            {
                { SvcParameter.Mandatory, "mandatory" },
                { SvcParameter.Alpn, "alpn" },
                { SvcParameter.NoDefaultAlpn, "no-default-alpn" },
                { SvcParameter.Port, "port" },
                { SvcParameter.IPv4Hint, "ipv4hint" },
                { SvcParameter.Ech, "ech" },
                { SvcParameter.IPv6Hint, "ipv6hint" }
            };

            foreach (var parameter in SvcParameters.Where(x => !(x.Value is DnsUnknownResource)).OrderBy(x => x.Key))
            {
                var keyName = labelMap.ContainsKey(parameter.Key) ? labelMap[parameter.Key] : $"key{(ushort)parameter.Key}";
                var keyValue = parameter.Value is SvcStringParameter || parameter.Value is SvcUtf8StringParameter ? $"\"{parameter.Value}\"" : parameter.Value.ToString();
                parts.Add($"{keyName}={keyValue}");
            }

            return string.Join(" ", parts);
        }
    }
}
