using System;
using System.Collections.Generic;

namespace Ae.Dns.Protocol.Resources
{
    public sealed class DnsSoaResource : IDnsResource, IEquatable<DnsSoaResource>
    {
        public string MName { get; set; }
        public string RName { get; set; }
        public uint Serial { get; set; }
        public TimeSpan Refresh { get; set; }
        public TimeSpan Retry { get; set; }
        public TimeSpan Expire { get; set; }
        public TimeSpan Minimum { get; set; }

        public bool Equals(DnsSoaResource other) => MName == other.MName && RName == other.RName && Serial == other.Serial && Refresh == other.Refresh && Retry == other.Retry && Expire == other.Expire && Minimum == other.Minimum;

        public override bool Equals(object obj) => obj is DnsSoaResource record ? Equals(record) : base.Equals(obj);

        public override int GetHashCode() => HashCode.Combine(MName, RName, Serial, Refresh, Retry, Expire, Minimum);

        public void ReadBytes(byte[] bytes, ref int offset, int length)
        {
            MName = string.Join(".", bytes.ReadString(ref offset));
            RName = string.Join(".", bytes.ReadString(ref offset));
            Serial = bytes.ReadUInt32(ref offset);
            Refresh = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Retry = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Expire = TimeSpan.FromSeconds(bytes.ReadInt32(ref offset));
            Minimum = TimeSpan.FromSeconds(bytes.ReadUInt32(ref offset));
        }

        public IEnumerable<IEnumerable<byte>> WriteBytes()
        {
            yield return MName.Split('.').ToBytes();
            yield return RName.Split('.').ToBytes();
            yield return Serial.ToBytes();
            yield return ((int)Refresh.TotalSeconds).ToBytes();
            yield return ((int)Retry.TotalSeconds).ToBytes();
            yield return ((int)Expire.TotalSeconds).ToBytes();
            yield return ((uint)Minimum.TotalSeconds).ToBytes();
        }
    }
}
