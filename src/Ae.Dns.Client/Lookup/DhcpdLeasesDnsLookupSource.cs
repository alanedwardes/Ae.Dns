﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Provides a <see cref="IDnsLookupSource"/> which parses DHCPD leases files,
    /// reading the IP address and hostname, and adding it to the lookup table
    /// for DNS resolution.
    /// </summary>
    public sealed class DhcpdLeasesDnsLookupSource : FileDnsLookupSource
    {
        private readonly string _hostnameSuffix;

        /// <summary>
        /// Construct a new <see cref="DhcpdLeasesDnsLookupSource"/> using the specified <see cref="FileInfo"/> and suffix string (for example, local).
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public DhcpdLeasesDnsLookupSource(ILogger<DhcpdLeasesDnsLookupSource> logger, FileInfo file, string hostnameSuffix = null) : base(logger, file)
        {
            _hostnameSuffix = hostnameSuffix;
            ReloadFile();
        }

        /// <summary>
        /// Construct a new <see cref="DhcpdLeasesDnsLookupSource"/> using the specified <see cref="FileInfo"/> and suffix string (for example, local).
        /// </summary>
        public DhcpdLeasesDnsLookupSource(FileInfo file, string hostnameSuffix = null)
            : this(NullLogger<DhcpdLeasesDnsLookupSource>.Instance, file, hostnameSuffix)
        {
        }

        /// <inheritdoc/>
        protected override IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sr)
        {
            IPAddress address = null;
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine().Trim();
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("lease"))
                {
                    var parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                    address = IPAddress.Parse(parts[1]);
                }

                if (address != null && line.StartsWith("client-hostname"))
                {
                    var parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                    var hostname = parts[1].Trim(new char[] { '"', ';' });

                    if (_hostnameSuffix != null)
                    {
                        yield return ($"{hostname}.{_hostnameSuffix}", address);
                    }
                    else
                    {
                        yield return (hostname, address);
                    }
                }
            }
        }
    }
}
