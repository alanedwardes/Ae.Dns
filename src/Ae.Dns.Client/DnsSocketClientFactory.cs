using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Ae.Dns.Client
{
    public sealed class DnsSocketClientFactory : IDnsSocketClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DnsSocketClientFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IDnsClient CreateUdpClient(IPAddress address) => new DnsUdpClient(_serviceProvider.GetRequiredService<ILogger<DnsUdpClient>>(), address);

        [Obsolete("This class is experimental.")]
        public IDnsClient CreateTcpClient(IPAddress address) => new DnsTcpClient(_serviceProvider.GetRequiredService<ILogger<DnsTcpClient>>(), address);
    }
}
