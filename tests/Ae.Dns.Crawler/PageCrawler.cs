using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<Uri, object> _addresses = new();

        public PageCrawler(ILogger logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

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
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(address);
            }
            catch (TaskCanceledException e)
            {
                _logger.LogError(e, "Unable to obtain page {Address}", address);
                return new HashSet<Uri>();
            }

            using (response)
            {
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
}
