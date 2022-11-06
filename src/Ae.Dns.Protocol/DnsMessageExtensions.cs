using Ae.Dns.Protocol.Enums;
using System.Linq;

namespace Ae.Dns.Protocol
{
    internal static class DnsMessageExtensions
    {
        public static ushort? GetMaxUdpMessageSize(this DnsMessage message)
        {
            return (ushort?)message.Additional.FirstOrDefault(x => x.Type == DnsQueryType.OPT)?.Class;
        }
    }
}
