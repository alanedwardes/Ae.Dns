using System.Collections.Generic;

namespace Ae.DnsResolver.Protocol.Records
{
    public sealed class DnsTextRecord : DnsResourceRecord
    {
        public string Text { get; set; }

        protected override void ReadBytes(byte[] bytes, ref int offset)
        {
            Text = string.Join('.', bytes.ReadString(ref offset, offset + DataLength));
        }

        protected override IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Text.Split('.').ToBytes();
        }
    }
}
