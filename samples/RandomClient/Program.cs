using Ae.Dns.Client;
using Ae.Dns.Protocol;
using System.Net;

using IDnsClient cloudFlare1 = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
using IDnsClient cloudFlare2 = new DnsUdpClient(IPAddress.Parse("1.0.0.1"));
using IDnsClient google1 = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));
using IDnsClient google2 = new DnsUdpClient(IPAddress.Parse("8.8.4.4"));

using IDnsClient dnsClient = new DnsRandomClient(cloudFlare1, cloudFlare2, google1, google2);

// Run an A query for google.com
DnsMessage answer = await dnsClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer);