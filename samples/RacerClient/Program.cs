using Ae.Dns.Client;
using Ae.Dns.Protocol;
using System.Net;

using IDnsClient cloudFlare1 = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
using IDnsClient cloudFlare2 = new DnsUdpClient(IPAddress.Parse("1.0.0.1"));
using IDnsClient google1 = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));
using IDnsClient google2 = new DnsUdpClient(IPAddress.Parse("8.8.4.4"));

// Construct the race: you must pass at least two clients
IDnsClient dnsClient = new DnsRacerClient(cloudFlare1, cloudFlare2, google1, google2);

// Race two clients in parallel, use the fatest result, discard the slowest
DnsMessage answer = await dnsClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer);