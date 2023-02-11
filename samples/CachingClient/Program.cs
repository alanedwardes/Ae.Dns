using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Runtime.Caching;

using IDnsClient upstreamClient = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));

using var memoryCache = new MemoryCache("dns");

using IDnsClient cacheClient = new DnsCachingClient(NullLogger<DnsCachingClient>.Instance, upstreamClient, memoryCache);

// Run an A query for google.com
DnsMessage answer1 = await cacheClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer1);

// Get cached result for query since it is inside the TTL
DnsMessage answer2 = await cacheClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer2);