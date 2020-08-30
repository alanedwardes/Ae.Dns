using System.Collections.Generic;
using System.Net;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsIpAddressRecord : DnsResourceRecord
    {
        public IPAddress IPAddress { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset) => IPAddress = new IPAddress(bytes.ReadBytes(DataLength, ref offset));

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return IPAddress.GetAddressBytes();
        }
    }
}
