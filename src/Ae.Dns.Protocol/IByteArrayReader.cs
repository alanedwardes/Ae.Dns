namespace Ae.Dns.Protocol
{
    public interface IByteArrayReader : IByteArrayWriter
    {
        void ReadBytes(byte[] bytes, ref int offset);
    }
}
