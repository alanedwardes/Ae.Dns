using Microsoft.Extensions.DependencyInjection;

namespace Ae.Dns.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDnsClient(this IServiceCollection services)
        {
            return services.AddSingleton<IDnsSocketClientFactory, DnsSocketClientFactory>();
        }
    }
}
