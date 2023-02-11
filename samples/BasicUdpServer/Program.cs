using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

// Can use the HTTPS, UDP, round robin or caching clients - any IDnsClient
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Allow anything that isn't www.google.com
IDnsFilter dnsFilter = new DnsDelegateFilter(x => x.Header.Host != "www.google.com");

using IDnsClient filterClient = new DnsFilterClient(NullLogger<DnsFilterClient>.Instance, dnsFilter, dnsClient);

// Listen on 127.0.0.1
var serverOptions = new DnsUdpServerOptions
{
    Endpoint = new IPEndPoint(IPAddress.Loopback, 53)
};

// Create a "raw" client (efficiently deals with network buffers)
using IDnsRawClient rawClient = new DnsRawClient(NullLogger<DnsRawClient>.Instance, filterClient);

// Create the server
using IDnsServer server = new DnsUdpServer(rawClient, serverOptions);

// Listen until cancelled
await server.Listen();