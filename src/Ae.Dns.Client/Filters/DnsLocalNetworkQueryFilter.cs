﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ae.Dns.Client.Internal;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// A filter which disallows queries which only make sense on a local network.
    /// This can be used to stop local network queries hitting an upstream DNS server on the internet for example.
    /// </summary>
    public sealed class DnsLocalNetworkQueryFilter : IDnsFilter
    {
        private readonly IEnumerable<string> _reservedTopLevelDomainNames = new[]
        {
            // See https://www.rfc-editor.org/rfc/rfc2606.html
            ".test", ".example", ".invalid", ".localhost",
            // See https://www.ietf.org/archive/id/draft-chapin-rfc2606bis-00.html
            ".local", ".localdomain", ".domain", ".lan", ".host",
            // See https://www.icann.org/resources/board-material/resolutions-2018-02-04-en#2.c
            ".home", ".corp", ".mail",
            // See https://www.rfc-editor.org/rfc/rfc6762#appendix-G
            ".intranet", ".internal", ".private"
        };

        // See https://www.ietf.org/rfc/rfc6763.txt
        private readonly IEnumerable<string> _reservedDnsServiceDiscoveryPrefixes = new[]
        {
            "b._dns-sd._udp.",
            "db._dns-sd._udp.",
            "r._dns-sd._udp.",
            "dr._dns-sd._udp.",
            "lb._dns-sd._udp."
        };


        /// <inheritdoc/>
        public bool IsPermitted(DnsMessage query)
        {
            // Very unlikely localhost got this far, though just in case it did
            if (query.Header.Host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(Host is localhost)";
                return false;
            }

            // Do not permit DNS-SD (service discovery) queries
            if (_reservedDnsServiceDiscoveryPrefixes.Any(x => query.Header.Host.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(DNS service discovery)";
                return false;
            }

            // Do not permit reserved top level domain names
            if (_reservedTopLevelDomainNames.Any(x => query.Header.Host.EndsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(Reserved local network TLD)";
                return false;
            }

            // Sometimes misconfigured applications can send queries beginning with a protocol
            if (query.Header.Host.StartsWith("https://") || query.Header.Host.StartsWith("http://"))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(Host contains http:// prefix)";
                return false;
            }

            var hostParts = query.Header.Host.Split('.');

            // Disallow a TLD containing an underscore
            if (hostParts.Last().Contains("_"))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(TLD contains underscore)";
                return false;
            }

            // Disallow something without a TLD
            // Note: sometimes this is valid, but it's so rare it's not worth allowing
            if (hostParts.Length < 2)
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(No TLD)";
                return false;
            }

            // Disallow reverse DNS lookups for private IP addresses
            if (query.TryParseIpAddressFromReverseLookup(out var address) && IpAddressExtensions.IsPrivate(address))
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(Reverse lookup for private IP address)";
                return false;
            }

            // See https://www.ietf.org/archive/id/draft-pauly-add-resolver-discovery-01.html
            // If you're running your own server on a local network, you probably don't want clients
            // bypassing the server and going directly to the upstream (if one happens to respond to this)
            if (query.Header.QueryType == DnsQueryType.SVCB && query.Header.Host == "_dns.resolver.arpa")
            {
                query.Header.Tags["BlockReason"] = $"{nameof(DnsLocalNetworkQueryFilter)}(DNS resolver discovery)";
                return false;
            }

            return true;
        }
    }
}
