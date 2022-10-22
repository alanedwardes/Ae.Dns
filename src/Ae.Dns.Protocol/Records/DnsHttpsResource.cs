using System;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsHttpsResource : IDnsResource, IEquatable<DnsHttpsResource>
    {
        public ushort SvcPriority { get; set; }

        public string TargetName { get; set; }

        public IList<(ushort, IList<string>)> SvcParams { get; set; } = new List<(ushort, IList<string>)>();

        /// <inheritdoc/>
        public bool Equals(DnsHttpsResource other) => false;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsHttpsResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public void ReadBytes(ReadOnlySpan<byte> bytes, ref int offset, int length)
        {
            SvcPriority = DnsByteExtensions.ReadUInt16(bytes, ref offset);
            TargetName = string.Join(".", DnsByteExtensions.ReadString(bytes, ref offset));

            while (offset < bytes.Length)
            {
                var key = DnsByteExtensions.ReadUInt16(bytes, ref offset);
                var l = DnsByteExtensions.ReadUInt16(bytes, ref offset);
                var str = DnsByteExtensions.ReadString(bytes, ref offset);
                SvcParams.Add((key, str));
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return DnsByteExtensions.ToBytes(SvcPriority);
            yield return DnsByteExtensions.ToBytes(TargetName.Split('.'));

            foreach (var key in SvcParams)
            {
                yield return DnsByteExtensions.ToBytes(key.Item1);
                yield return DnsByteExtensions.ToBytes((ushort)key.Item2.Sum(x => x.Length));
                yield return DnsByteExtensions.ToBytes(key.Item2);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => TargetName;
    }
}
