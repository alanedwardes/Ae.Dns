namespace Ae.Dns.Protocol
{
    public interface IDnsByteArrayReader : IDnsByteArrayWriter
    {
        void ReadBytes(byte[] bytes, ref int offset);
    }
}
