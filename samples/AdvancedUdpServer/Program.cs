using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Runtime.Caching;

using IDnsClient cloudFlare1 = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
using IDnsClient cloudFlare2 = new DnsUdpClient(IPAddress.Parse("1.0.0.1"));
using IDnsClient google1 = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));
using IDnsClient google2 = new DnsUdpClient(IPAddress.Parse("8.8.4.4"));

// Aggregate all clients into one
using IDnsClient roundRobinClient = new DnsRoundRobinClient(cloudFlare1, cloudFlare2, google1, google2);

// Add the caching layer
using IDnsClient cacheClient = new DnsCachingClient(roundRobinClient, new MemoryCache("dns"));

using var httpClient = new HttpClient();

// Create a remote blocklist filter
var remoteSetFilter = new DnsRemoteSetFilter(httpClient);

// Block all domains from https://github.com/StevenBlack/hosts
// don't await the task to allow the server to start (class is thread safe)
var hostsFile = new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts");
_ = remoteSetFilter.AddRemoteBlockList(hostsFile);

// Add the filtering layer
using IDnsClient filterClient = new DnsFilterClient(remoteSetFilter, cacheClient);

// Listen on 127.0.0.1
var serverOptions = new DnsUdpServerOptions
{
    Endpoint = new IPEndPoint(IPAddress.Loopback, 53)
};

// Create a "raw" client (efficiently deals with network buffers)
using IDnsRawClient rawClient = new DnsRawClient(filterClient);

// Create the server using the caching client and remote set filter
using IDnsServer server = new DnsUdpServer(rawClient, serverOptions);

// Listen until cancelled
await server.Listen();