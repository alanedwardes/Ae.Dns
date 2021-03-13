using Ae.Dns.Protocol;
using System.Net;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Represents a <see cref="IDnsClient"/> factory for TCP and UDP.
    /// </summary>
    public interface IDnsSocketClientFactory
    {
        /// <summary>
        /// Create a <see cref="IDnsClient"/> using TCP.
        /// </summary>
        /// <param name="address">The address of the server to use.</param>
        /// <returns>A new <see cref="IDnsClient"/> ready for use.</returns>
        IDnsClient CreateTcpClient(IPAddress address);
        /// <summary>
        /// Create a <see cref="IDnsClient"/> using UDP.
        /// </summary>
        /// <param name="address">The address of the server to use.</param>
        /// <returns>A new <see cref="IDnsClient"/> ready for use.</returns>
        IDnsClient CreateUdpClient(IPAddress address);
    }
}