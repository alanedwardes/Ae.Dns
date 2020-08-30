using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class UnimplementedDnsResourceRecord : DnsResourceRecord
    {
        public byte[] Raw { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset) => Raw = bytes.ReadBytes(DataLength, ref offset);

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Raw;
        }
    }
}
