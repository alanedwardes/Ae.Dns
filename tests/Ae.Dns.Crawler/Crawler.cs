using Ae.Dns.Client.Exceptions;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Tests;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.Dns.Benchmarks
{
    public sealed class Crawler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDnsClient _dnsClient;
        private readonly ILogger<Crawler> _logger;

        public Crawler(IHttpClientFactory httpClientFactory, IDnsClient dnsClient, ILogger<Crawler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _dnsClient = dnsClient;
            _logger = logger;
        }

        public async Task Crawl()
        {
            using var client = _httpClientFactory.CreateClient("Crawler");

            var pageCrawler = new PageCrawler(client);

            var foundDomains = new HashSet<string>();

            await pageCrawler.AddAddressesFromPage(new Uri("https://news.ycombinator.com/"));

            var tasks = new List<Task>();

            foreach (var address in pageCrawler.Addresses)
            {
                if (foundDomains.Add(address.Host))
                {
                    tasks.Add(pageCrawler.AddAddressesFromPage(address));
                }
            }

            await Task.WhenAll(tasks);

            foreach (var address in pageCrawler.Addresses)
            {
                foundDomains.Add(address.Host);
            }

            var answers = new List<DnsMessage>();

            foreach (var domain in foundDomains)
            {
                if (IPAddress.TryParse(domain, out _))
                {
                    continue;
                }

                try
                {
                    var answer = await _dnsClient.Query(DnsQueryFactory.CreateQuery(domain, DnsQueryType.ANY));
                    if (answer.Header.ResponseCode == DnsResponseCode.NotImp)
                    {
                        continue;
                    }
                    answers.Add(answer);
                }
                catch (DnsClientException e)
                {
                    _logger.LogCritical(e, "Error making request for {Domain}", domain);
                }
            }

            _logger.LogInformation("Writing answers");

            using (var fs = File.Open("answers.bin", FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var answer in answers)
                {
                    WriteAnswer(bw, answer);
                }
            }

            static void WriteAnswer(BinaryWriter bw, DnsMessage answer)
            {
                var buffer = DnsByteExtensions.AllocateAndWrite(answer);

                bw.Write(buffer.Length);
                bw.Write(buffer);
            }
        }
    }
}
