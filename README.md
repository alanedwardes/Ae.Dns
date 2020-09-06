# Ae.Dns
![.NET Core](https://github.com/alanedwardes/Ae.Dns/workflows/.NET%20Core/badge.svg?branch=main)

A pure C# implementation of a DNS client, server and configurable caching/filtering layer. This project offers the following packages:
* [Ae.Dns.Client](#aednsclient) - HTTP and UDP DNS clients with caching and round-robin capabilities
* [Ae.Dns.Server](#aednsserver) - UDP DNS server with filtering capabilities
* [Ae.Dns.Protocol](#aednsprotocol) - Low level DNS wire protocol round-trip handling used on the client and server

## Ae.Dns.Client
[![](https://img.shields.io/nuget/v/Ae.Dns.Client)](https://www.nuget.org/packages/Ae.Dns.Client/)

Offers both HTTP(S) and UDP DNS clients, with a simple round robin client implementation.
### Basic HTTPS Client Usage
This example is a very simple setup of the `DnsHttpClient` using the CloudFlare DNS over HTTPS resolver.
```csharp
using var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://cloudflare-dns.com/")
};
IDnsClient dnsClient = new DnsHttpClient(httpClient);

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```
### Advanced HTTPS Client Usage
This example uses [`Microsoft.Extensions.Http.Polly`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly/) to allow retrying of failed requests.
```csharp
IServiceCollection services = new ServiceCollection();

var dnsUri = new Uri("https://cloudflare-dns.com/");
services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = dnsUri)
        .AddTransientHttpErrorPolicy(x => x.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

IServiceProvider provider = services.BuildServiceProvider();

IDnsClient dnsClient = provider.GetRequiredService<IDnsClient>();

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```
### Basic UDP Client Usage
This example is a basic setup of the `DnsUdpClient` using the primary CloudFlare UDP resolver.
```csharp
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"), CancellationToken.None);
```

### Round Robin Client Usage
This example uses multiple upstream `IDnsClient` implementations in a round-robin fashion using `DnsRoundRobinClient`. Note that multiple protocols can be mixed, both the `DnsUdpClient` and `DnsHttpClient` can be used here since they implement `IDnsClient`.

```csharp
using IDnsClient cloudFlare1 = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
using IDnsClient cloudFlare2 = new DnsUdpClient(IPAddress.Parse("1.0.0.1"));
using IDnsClient google1 = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));
using IDnsClient google2 = new DnsUdpClient(IPAddress.Parse("8.8.4.4"));

IDnsClient dnsClient = new DnsRoundRobinClient(cloudFlare1, cloudFlare2, google1, google2);

// Run an A query for google.com
DnsAnswer answer = await dnsClient.Query(DnsHeader.CreateQuery("google.com"));
```

### Caching Client Usage
This example uses the `DnsCachingClient` to cache queries into a `MemoryCache`, so that the answer is not retrieved from the upstream if the answer is within its TTL. Note that this can be combined with the `DnsRoundRobinClient`, so the cache can be backed by multiple upstream clients (it just accepts `IDnsClient`).

```csharp
using IDnsClient upstreamClient = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));

using var memoryCache = new MemoryCache("dns");

IDnsClient cacheClient = new DnsCachingClient(upstreamClient, memoryCache);

// Run an A query for google.com
DnsAnswer answer1 = await cacheClient.Query(DnsHeader.CreateQuery("google.com"));

// Get cached result for query since it is inside the TTL
DnsAnswer answer2 = await cacheClient.Query(DnsHeader.CreateQuery("google.com"));
```

## Ae.Dns.Server
[![](https://img.shields.io/nuget/v/Ae.Dns.Client)](https://www.nuget.org/packages/Ae.Dns.Server/)

Offers a UDP server with filtering capabilities.

### Basic UDP Server
A example UDP server which forwards all queries via UDP to the CloudFlare DNS resolver, and blocks `www.google.com`.

```csharp
// Can use the HTTPS, UDP, round robin or caching clients - any IDnsClient
using IDnsClient dnsClient = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));

// Allow anything that isn't www.google.com
IDnsFilter dnsFilter = new DnsDelegateFilter(x => x.Host != "www.google.com");

// Create the server
using IDnsServer server = new DnsUdpServer(new IPEndPoint(IPAddress.Any, 53), dnsClient, dnsFilter);

// Listen until cancelled
await server.Listen(CancellationToken.None);
```

### Advanced UDP Server
A more advanced UDP server which uses the `DnsCachingClient` and `DnsRoundRobinClient` to cache DNS answers from multiple upstream clients, and the `RemoteSetFilter` to block domains from a remote hosts file.

```csharp
using IDnsClient cloudFlare1 = new DnsUdpClient(IPAddress.Parse("1.1.1.1"));
using IDnsClient cloudFlare2 = new DnsUdpClient(IPAddress.Parse("1.0.0.1"));
using IDnsClient google1 = new DnsUdpClient(IPAddress.Parse("8.8.8.8"));
using IDnsClient google2 = new DnsUdpClient(IPAddress.Parse("8.8.4.4"));

// Aggregate all clients into one
IDnsClient roundRobinClient = new DnsRoundRobinClient(cloudFlare1, cloudFlare2, google1, google2);

// Add the caching layer
IDnsClient cacheClient = new DnsCachingClient(roundRobinClient, new MemoryCache("dns"));

// Create a remote blocklist filter
var remoteSetFilter = new DnsRemoteSetFilter();

// Block all domains from https://github.com/StevenBlack/hosts
// don't await the task to allow the server to start (class is thread safe)
_ = remoteSetFilter.AddRemoteBlockList(new Uri("https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts"));

// Add the filtering layer
IDnsClient filterClient = new DnsFilterClient(remoteSetFilter, cacheClient);

// Create the server using the caching client and remote set filter
using IDnsServer server = new DnsUdpServer(new IPEndPoint(IPAddress.Any, 53), filterClient);

// Listen until cancelled
await server.Listen(CancellationToken.None);
```

## Ae.Dns.Protocol
[![](https://img.shields.io/nuget/v/Ae.Dns.Protocol)](https://www.nuget.org/packages/Ae.Dns.Protocol/)

Provides the low-level protocol handling for the over-the-wire DNS protocol format as per [RFC 1035](https://tools.ietf.org/html/rfc1035).

See the following types:
* DnsHeader
* DnsAnswer
* DnsResourceRecord
* DnsIpAddressResource
* DnsMxResource
* DnsSoaResource
* DnsTextResource
* DnsUnknownResource
