using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ae.Dns.Tests
{
    public sealed class PageCrawler
    {
        private static readonly Regex _regex = new(@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)");
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<Uri, object> _addresses = new();

        public PageCrawler(HttpClient httpClient) => _httpClient = httpClient;

        public IReadOnlySet<Uri> Addresses => new HashSet<Uri>(_addresses.Keys);

        public async Task AddAddressesFromPage(Uri pageAddress)
        {
            foreach (var uri in await GetDomainsOnPageInternal(pageAddress))
            {
                _addresses.TryAdd(uri, null);
            }
        }

        private async Task<IReadOnlySet<Uri>> GetDomainsOnPageInternal(Uri address)
        {
            using var response = await _httpClient.GetAsync(address);

            if (!response.IsSuccessStatusCode)
            {
                return new HashSet<Uri>();
            }

            var matches = _regex.Matches(await response.Content.ReadAsStringAsync());

            var uris = new HashSet<Uri>();
            foreach (var match in matches)
            {
                if (Uri.TryCreate(match.ToString(), UriKind.Absolute, out var uri))
                {
                    uris.Add(uri);
                }
            }

            return uris;
        }
    }
}
