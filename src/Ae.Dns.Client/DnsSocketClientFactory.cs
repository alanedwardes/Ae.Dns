using Ae.Dns.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Ae.Dns.Client
{
    /// <inheritdoc/>
    public sealed class DnsSocketClientFactory : IDnsSocketClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Create a new <see cref="DnsSocketClientFactory"/> using the specified <see cref="IServiceProvider"/> to retrieve loggers.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public DnsSocketClientFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        /// <inheritdoc/>
        public IDnsClient CreateUdpClient(IPAddress address) => new DnsUdpClient(_serviceProvider.GetRequiredService<ILogger<DnsUdpClient>>(), address);

        /// <inheritdoc/>
        [Obsolete("This class is experimental.")]
        public IDnsClient CreateTcpClient(IPAddress address) => new DnsTcpClient(_serviceProvider.GetRequiredService<ILogger<DnsTcpClient>>(), address);
    }
}
