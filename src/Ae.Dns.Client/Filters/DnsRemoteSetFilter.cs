using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Filters
{
    /// <summary>
    /// Provides the ability to use remote block lists
    /// (hosts files or line-delimited lists of domains)
    /// to deny DNS queries.
    /// </summary>
    public sealed class DnsRemoteSetFilter : IDnsFilter
    {
        private readonly ConcurrentDictionary<string, bool> _domains = new ConcurrentDictionary<string, bool>();
        private readonly ILogger<DnsRemoteSetFilter> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Create a new DNS remote set filter instance with a null logger.
        /// </summary>
        public DnsRemoteSetFilter() : this(new NullLogger<DnsRemoteSetFilter>())
        {
        }

        /// <summary>
        /// Create a new DNS remote set filter instance with the specified logger.
        /// </summary>
        public DnsRemoteSetFilter(ILogger<DnsRemoteSetFilter> logger) : this(logger, new HttpClient())
        {
        }

        /// <summary>
        /// Create a new DNS remote set filter instance with the specified logger and HttpClient.
        /// </summary>
        public DnsRemoteSetFilter(ILogger<DnsRemoteSetFilter> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private async Task AddRemoteList(Uri fileUri, bool allow)
        {
            var set = new HashSet<string>();

            _logger.LogTrace("Downloading {FilterUri}", fileUri);

            var response = await _httpClient.GetStreamAsync(fileUri);
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

        /// <summary>
        /// Add the specified URL pointing to a list of domains to deny queries against.
        /// </summary>
        public Task AddRemoteBlockList(Uri hostsFileUri) => AddRemoteList(hostsFileUri, false);

        /// <summary>
        /// Add the specified URL pointing to a list of domains to allow queries against.
        /// </summary>
        public Task AddRemoteAllowList(Uri hostsFileUri) => AddRemoteList(hostsFileUri, true);

        /// <inheritdoc/>
        public bool IsPermitted(DnsMessage query)
        {
            if (_domains.TryGetValue(query.Header.Host, out bool allowed))
            {
                return allowed;
            }

            return true;
        }
    }
}
