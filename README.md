# Ae.Dns ![.NET Core](https://github.com/alanedwardes/Ae.Dns/workflows/.NET%20Core/badge.svg?branch=main)
**API Documentation: https://alanedwardes.github.io/docs/Ae.Dns/**

A pure C# implementation of a DNS client, server and configurable caching/filtering layer. This project offers the following packages:
* [Ae.Dns.Client](#aednsclient) - HTTP and UDP DNS clients with caching capabilities
* [Ae.Dns.Server](#aednsserver) - Standard UDP DNS server
* [Ae.Dns.Server.Http](#aednsserverhttp) - HTTP DNS server (if coupled with SSL, a DoH server)
* [Ae.Dns.Protocol](#aednsprotocol) - Low level DNS wire protocol round-trip handling used on the client and server

## Ae.Dns.Client
[![](https://img.shields.io/nuget/v/Ae.Dns.Client) ![](https://img.shields.io/badge/framework-netstandard2.0-blue)](https://www.nuget.org/packages/Ae.Dns.Client/) 

Offers both HTTP(S) and UDP DNS clients, with a simple random client implementation.
### Basic HTTPS Client Usage
This example is a very simple setup of the `DnsHttpClient` using the CloudFlare DNS over HTTPS resolver.

See [samples/BasicHttpClient/Program.cs](samples/BasicHttpClient/Program.cs)

### Advanced HTTPS Client Usage
This example uses [`Microsoft.Extensions.Http.Polly`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly/) to allow retrying of failed requests.

See [samples/AdvancedHttpClient/Program.cs](samples/AdvancedHttpClient/Program.cs)

### Basic UDP Client Usage
This example is a basic setup of the `DnsUdpClient` using the primary CloudFlare UDP resolver.

See [samples/BasicUdpClient/Program.cs](samples/BasicUdpClient/Program.cs)

### Random Client Usage
This example uses multiple upstream `IDnsClient` implementations in a random fashion using `DnsRandomClient`. Note that multiple protocols can be mixed, both the `DnsUdpClient` and `DnsHttpClient` can be used here since they implement `IDnsClient`.

See [samples/RandomClient/Program.cs](samples/RandomClient/Program.cs)

### Racer Client Usage
This example uses multiple (two or more) upstream `IDnsClient` implementations and races them using `DnsRacerClient`. Two queries are sent in parallel, and the fastest result "wins" (is returned). If a query faults, the other query result is used. As with the random client, multiple protocols can be mixed, both the `DnsUdpClient` and `DnsHttpClient` can be used here since they implement `IDnsClient`.

See [samples/RacerClient/Program.cs](samples/RacerClient/Program.cs)

### Caching Client Usage
This example uses the `DnsCachingClient` to cache queries into a `MemoryCache`, so that the answer is not retrieved from the upstream if the answer is within its TTL. Note that this can be combined with the `DnsRandomClient`, so the cache can be backed by multiple upstream clients (it just accepts `IDnsClient`).

See [samples/CachingClient/Program.cs](samples/CachingClient/Program.cs)

## Ae.Dns.Server
[![](https://img.shields.io/nuget/v/Ae.Dns.Server) ![](https://img.shields.io/badge/framework-netstandard2.0-blue)](https://www.nuget.org/packages/Ae.Dns.Server/)

Offers a standard DNS UDP server.

### Basic UDP Server
A example UDP server which forwards all queries via UDP to the CloudFlare DNS resolver, and blocks `www.google.com`.

See [samples/BasicUdpServer/Program.cs](samples/BasicUdpServer/Program.cs)

### Advanced UDP Server
A more advanced UDP server which uses the `DnsCachingClient` and `DnsRandomClient` to cache DNS answers from multiple upstream clients, and the `RemoteSetFilter` to block domains from a remote hosts file.

See [samples/AdvancedUdpServer/Program.cs](samples/AdvancedUdpServer/Program.cs)

## Ae.Dns.Server.Http
[![](https://img.shields.io/nuget/v/Ae.Dns.Server.Http) ![](https://img.shields.io/badge/framework-netcoreapp3.1-blue)](https://www.nuget.org/packages/Ae.Dns.Server.Http/)

Offers an HTTPS (DoH) server for use with clients supporting the DNS over HTTPS protocol.

See [samples/BasicHttpServer/Program.cs](samples/BasicHttpServer/Program.cs)

## Ae.Dns.Protocol
[![](https://img.shields.io/nuget/v/Ae.Dns.Protocol) ![](https://img.shields.io/badge/framework-netstandard2.0-blue)](https://www.nuget.org/packages/Ae.Dns.Protocol/)

Provides the low-level protocol handling for the over-the-wire DNS protocol format as per [RFC 1035](https://tools.ietf.org/html/rfc1035).

### HttpClient DNS Resolution Middleware

This package includes a `DelegatingHandler` for `HttpClient` which intercepts HTTP requests before they're sent across the wire, resolves the host with the specified `IDnsClient`, replaces the host with one of the resolved IP addresses, then sends the request to the IP address with the host as the `Host` header.

#### Basic DnsDelegatingHandler Example

See [samples/BasicHttpClientMiddleware/Program.cs](samples/BasicHttpClientMiddleware/Program.cs)

#### Advanced DnsDelegatingHandler Dependency Injection Example

See [samples/AdvancedHttpClientMiddleware/Program.cs](samples/AdvancedHttpClientMiddleware/Program.cs)
