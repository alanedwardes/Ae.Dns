using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Polly;

IServiceCollection services = new ServiceCollection();

var dnsUri = new Uri("https://cloudflare-dns.com/");
services.AddHttpClient<IDnsClient, DnsHttpClient>(x => x.BaseAddress = dnsUri)
        .AddTransientHttpErrorPolicy(x =>
            x.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

using ServiceProvider provider = services.BuildServiceProvider();

using IDnsClient dnsClient = provider.GetRequiredService<IDnsClient>();

// Run an A query for google.com
DnsMessage answer = await dnsClient.Query(DnsQueryFactory.CreateQuery("google.com"));

Console.WriteLine(answer);