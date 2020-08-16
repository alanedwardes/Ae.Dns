using Ae.DnsResolver.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public sealed class DnsRemoteSetFilter : IDnsFilter
    {
        private readonly ISet<string> _domains = new HashSet<string>();

        public async Task AddRemoteList(Uri hostsFileUri)
        {
            var set = new HashSet<string>();

            using var httpClient = new HttpClient();
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

            lock (_domains)
            {
                foreach (var domain in set)
                {
                    _domains.Add(domain);
                }
            }
        }

        public bool IsPermitted(DnsHeader query)
        {
            var domain = string.Join(".", query.Labels);
            return !_domains.Contains(domain);
        }
    }
}
