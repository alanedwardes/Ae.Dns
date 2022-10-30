using System;
namespace Ae.Dns.Protocol.Records
{
    public class DnsOptResource : IDnsResource
    {
        /// <summary>
        /// The raw bytes recieved for this DNS resource.
        /// </summary>
        /// <value>
        /// The raw set of bytes representing the value fo this DNS resource.
        /// For string values, due to compression the whole packet may be needed.
        /// </value>
        public ReadOnlyMemory<byte> Raw { get; set; }

        /// <summary>
        /// The maximum UDP payload size a requestor can receive (without fragmentation at the IP layer).
        /// Packets larger than this size should be truncated and the requestor should re-request via TCP or DoH.
        /// </summary>
        /// <value>
        /// See https://www.rfc-editor.org/rfc/rfc6891#page-10 for more information
        /// </value>
        public ushort MaximumPayloadSize { get; set; }

        /// <summary>
        /// See https://www.rfc-editor.org/rfc/rfc6891#section-6.1.3 for more information.
        /// </summary>
        public uint Flags { get; set; }

        public void ReadBytes(ReadOnlyMemory<byte> bytes, ref int offset, int length)
        {
            // This type of DNS record uses the class, ttl and rdlen data
            // This code is not designed for that right now, so we'll just look backwards to get the data again
            // See https://www.rfc-editor.org/rfc/rfc6891#section-6.1.2
            var rdlenOffset = offset - sizeof(ushort);
            var ttlOffset = rdlenOffset - sizeof(uint);
            var classOffset = ttlOffset - sizeof(ushort);

            MaximumPayloadSize = DnsByteExtensions.ReadUInt16(bytes, ref classOffset);
            Flags = DnsByteExtensions.ReadUInt32(bytes, ref ttlOffset);
            Raw = DnsByteExtensions.ReadBytes(bytes, length, ref offset);
        }

        public void WriteBytes(Memory<byte> bytes, ref int offset)
        {
            // Payloadsize / flags is serialised at a higher level
            Raw.CopyTo(bytes.Slice(offset));
            offset += Raw.Length;
        }
    }
}

