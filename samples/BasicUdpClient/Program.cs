using Ae.Dns.Client;
using Ae.Dns.Protocol;
using System.Net;

using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Run an A query for google.com
DnsMessage answer = await dnsClient.Query(DnsQueryFactory.CreateQuery("google.com"), CancellationToken.None);

Console.WriteLine(answer);