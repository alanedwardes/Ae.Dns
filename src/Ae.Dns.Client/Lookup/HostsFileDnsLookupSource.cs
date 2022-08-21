using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// Provides a <see cref="IDnsLookupSource"/> which monitors the specified hosts file.
    /// </summary>
    public sealed class HostsFileDnsLookupSource : FileDnsLookupSource
    {
        /// <summary>
        /// Construct a new <see cref="HostsFileDnsLookupSource"/> using the specified <see cref="FileInfo"/>.
        /// </summary>
        public HostsFileDnsLookupSource(ILogger<HostsFileDnsLookupSource> logger, FileInfo file) : base(logger, file)
        {
            ReloadFile();
        }

        /// <inheritdoc/>
        protected override IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sr)
        {
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                yield return (parts[1], IPAddress.Parse(parts[0]));
            }
        }
    }
}
