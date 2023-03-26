using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Server.Http;
using System.Net;

// Can use the HTTPS, UDP, random or caching clients - any IDnsClient
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Create the HTTP server - see overloads for configuration options
using IDnsServer server = new DnsHttpServer(dnsClient);

// Listen until cancelled
await server.Listen();