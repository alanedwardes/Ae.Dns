using Ae.Dns.Client;
using Ae.Dns.Protocol;

using var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://cloudflare-dns.com/")
};

using IDnsClient dnsClient = new DnsHttpClient(httpClient);

// Run an A query for google.com
DnsMessage answer = await dnsClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer);