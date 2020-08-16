using Ae.DnsResolver.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public sealed class DnsRemoteSetFilter : IDnsFilter
    {
        private readonly ConcurrentDictionary<string, byte> _domains = new ConcurrentDictionary<string, byte>();
        private readonly ILogger<DnsRemoteSetFilter> _logger;

        public DnsRemoteSetFilter(ILogger<DnsRemoteSetFilter> logger)
        {
            _logger = logger;
        }

        public async Task AddRemoteList(Uri hostsFileUri)
        {
            var set = new HashSet<string>();

            using var httpClient = new HttpClient();

            _logger.LogTrace("Downloading {0}", hostsFileUri);

            var response = await httpClient.GetStreamAsync(hostsFileUri);
            using var sr = new StreamReader(response);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line.StartsWith("#"))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.Contains(" "))
                {
                    var domain = line.Replace("0.0.0.0", string.Empty).Trim();
                    set.Add(domain);
                }
                else
                {
                    set.Add(line.Trim());
                }
            }

            _logger.LogTrace("Found {0} domains in {1}", set.Count, hostsFileUri);

            foreach (var domain in set)
            {
                _domains.TryAdd(domain, 0);
            }

            _logger.LogInformation("Block list now contains {0} domains", _domains.Count);
        }

        public bool IsPermitted(DnsHeader query)
        {
            var domain = string.Join(".", query.Labels);
            return !_domains.ContainsKey(domain);
        }
    }
}
