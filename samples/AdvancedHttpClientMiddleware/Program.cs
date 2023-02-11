using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

// Create a DI container
IServiceCollection services = new ServiceCollection();

// Add any IDnsClient implementation to it
services.AddSingleton<IDnsClient>(new DnsUdpClient(IPAddress.Parse("1.1.1.1")));

// Add the DelegatingHandler
services.AddTransient<DnsDelegatingHandler>();

// Set up your HTTP client class
services.AddHttpClient<IMyTypedClient, MyTypedClient>()
        .AddHttpMessageHandler<DnsDelegatingHandler>();

// Build the service provider
IServiceProvider provider = services.BuildServiceProvider();

// Retrieve the HTTP client implementation
IMyTypedClient myTypedClient = provider.GetRequiredService<IMyTypedClient>();

var response = await myTypedClient.GetGoogle(CancellationToken.None);

Console.WriteLine(response.StatusCode);