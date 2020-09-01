namespace Ae.Dns.Protocol.Resources
{
    public interface IDnsResource : IByteArrayWriter
    {
        void ReadBytes(byte[] bytes, ref int offset, int length);
    }
}
