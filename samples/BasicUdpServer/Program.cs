﻿using Ae.Dns.Client;
using Ae.Dns.Client.Filters;
using Ae.Dns.Protocol;
using Ae.Dns.Server;
using System.Net;

// Can use the HTTPS, UDP, random or caching clients - any IDnsClient
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Allow anything that isn't www.google.com
IDnsFilter dnsFilter = new DnsDelegateFilter(x => x.Header.Host != "www.google.com");

using IDnsClient filterClient = new DnsFilterClient(dnsFilter, dnsClient);

// Listen on 127.0.0.1
var serverOptions = new DnsUdpServerOptions
{
    Endpoint = new IPEndPoint(IPAddress.Loopback, 53)
};

// Create a "raw" client (efficiently deals with network buffers)
using IDnsRawClient rawClient = new DnsRawClient(filterClient);

// Create the server
using IDnsServer server = new DnsUdpServer(rawClient, serverOptions);

// Listen until cancelled
await server.Listen();