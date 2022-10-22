﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    /// <summary>
    /// Represents a DNS resource that this library cannot yet understand.
    /// </summary>
    public sealed class DnsUnknownResource : IDnsResource, IEquatable<DnsUnknownResource>
    {
        /// <summary>
        /// The raw bytes recieved for this DNS resource.
        /// </summary>
        /// <value>
        /// The raw set of bytes representing the value fo this DNS resource.
        /// For string values, due to compression the whole packet may be needed.
        /// </value>
        public byte[] Raw { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsUnknownResource other) => Raw.SequenceEqual(other.Raw);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsUnknownResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Raw);

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset, int length)
        {
            Raw = DnsByteExtensions.ReadBytes(bytes, length, ref offset).ToArray();
        }

        /// <inheritdoc/>
        public override string ToString() => $"Raw {Raw.Length} bytes";

        /// <inheritdoc/>
        public void WriteBytes(Span<byte> bytes, ref int offset)
        {
            Raw.CopyTo(bytes.Slice(offset));
            offset += Raw.Length;
        }
    }
}
