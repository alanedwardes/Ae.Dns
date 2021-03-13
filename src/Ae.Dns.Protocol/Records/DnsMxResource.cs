using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Records
{
    public sealed class DnsMxResource : IDnsResource, IEquatable<DnsMxResource>
    {
        public ushort Preference { get; set; }
        public string Exchange { get; set; }

        /// <inheritdoc/>
        public bool Equals(DnsMxResource other) => Preference == other.Preference && Exchange == other.Exchange;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DnsMxResource record ? Equals(record) : base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Preference, Exchange);

        /// <inheritdoc/>
        public void ReadBytes(byte[] bytes, ref int offset, int length)
        {
            Preference = bytes.ReadUInt16(ref offset);
            Exchange = string.Join(".", bytes.ReadString(ref offset));
        }

        /// <inheritdoc/>
        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return Preference.ToBytes();
            yield return Exchange.Split('.').ToBytes();
        }
    }
}
