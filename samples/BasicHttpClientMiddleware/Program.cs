using System.Net;
using Ae.Dns.Protocol;
using Ae.Dns.Client;

// Can use the HTTPS, UDP, random or caching clients - any IDnsClient
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Create an HttpClient with the DnsDelegatingHandler
using HttpClient httpClient = new HttpClient(new DnsDelegatingHandler(dnsClient)
{
    InnerHandler = new SocketsHttpHandler()
});

// Make a request to GET www.google.com using the DNS middleware
HttpResponseMessage response = await httpClient.GetAsync("https://www.google.com/");

Console.WriteLine(response.StatusCode);