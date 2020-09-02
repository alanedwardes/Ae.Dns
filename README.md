# Ae.Dns
A C# implementation of a DNS client, server and configurable caching/filtering layer.
* [Ae.Dns.Client](#Ae.Dns.Client) - HTTP and UDP DNS clients

## Ae.Dns.Client
Offers both HTTP(S) and UDP DNS clients.
### Basic HTTPS Client Usage
This example is a very simple setup of the HTTP client using CloudFlare.
```csharp
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://cloudflare-dns.com/")
};
IDnsClient dnsClient = new DnsHttpClient(httpClient);

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```
### Advanced HTTPS Client Usage
This example uses `Microsoft.Extensions.Http.Polly` to allow retrying of failed requests.
```csharp
IServiceCollection services = new ServiceCollection();

var dnsUri = new Uri("https://cloudflare-dns.com/");
services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = dnsUri)
        .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError());

IServiceProvider provider = servics.BuildServiceProvider();

IDnsClient dnsClient = provider.GetRequiredService<IDnsClient>();

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```
### Basic UDP Client Usage
This example is a very setup of the UDP client using CloudFlare.
```csharp
var dnsAddress = IPAddress.Parse("1.1.1.1");

using IDnsClient dnsClient = new DnsUdpClient(new NullLogger<DnsUdpClient>(), dnsAddress);

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```