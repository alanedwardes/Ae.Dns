using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.Dns.Repository
{
    public sealed class DnsRemoteSetFilter : IDnsFilter
    {
        private readonly ConcurrentDictionary<string, bool> _domains = new ConcurrentDictionary<string, bool>();
        private readonly ILogger<DnsRemoteSetFilter> _logger;

        public DnsRemoteSetFilter(ILogger<DnsRemoteSetFilter> logger)
        {
            _logger = logger;
        }

        private async Task AddRemoteList(Uri fileUri, bool allow)
        {
            var set = new HashSet<string>();

            using var httpClient = new HttpClient();

            _logger.LogTrace("Downloading {FilterUri}", fileUri);

            var response = await httpClient.GetStreamAsync(fileUri);
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

            _logger.LogTrace("Found {Count} domains in {FilterUri}", set.Count, fileUri);

            foreach (var domain in set)
            {
                _domains[domain] = allow;
            }

            _logger.LogInformation("Filter list now contains {Count} domains", _domains.Count);
        }

        public Task AddRemoteBlockList(Uri hostsFileUri) => AddRemoteList(hostsFileUri, false);

        public Task AddRemoteAllowList(Uri hostsFileUri) => AddRemoteList(hostsFileUri, false);

        public bool IsPermitted(DnsHeader query)
        {
            if (_domains.TryGetValue(query.Host, out bool allowed))
            {
                return allowed;
            }

            return true;
        }
    }
}
