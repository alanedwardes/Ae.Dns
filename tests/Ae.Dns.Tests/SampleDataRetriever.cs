using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ae.Dns.Tests
{
    public static class SampleDataRetriever
    {
        private static readonly Regex _regex = new(@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)");
        private static readonly HttpClient _httpClient = new();
        private static readonly ConcurrentDictionary<string, Task<IReadOnlySet<string>>> _sets = new();

        public static Task<IReadOnlySet<string>> GetDomainsOnPage(string address) => _sets.GetOrAdd(address, x => GetDomainsOnPageInternal(x));

        private static async Task<IReadOnlySet<string>> GetDomainsOnPageInternal(string address)
        {
            var response = await _httpClient.GetStringAsync(address);

            var matches = _regex.Matches(response);

            var domains = new HashSet<string>();
            foreach (var match in matches)
            {
                if (Uri.TryCreate(match.ToString(), UriKind.Absolute, out var uri))
                {
                    domains.Add(uri.Host);
                }
            }

            return domains;
        }
    }
}
